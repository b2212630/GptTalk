using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class TexttoSpeach : MonoBehaviour
{
    //GPTの処理時間用
    public TextMeshProUGUI ProcessingTime;

    //音声再生中かどうか
    private bool isPlayingAudio = false;
    private float silenceTimer = 0f; // タイマー
    private bool isCheckingSilence = false; // 沈黙チェック中かどうか
    private const float silenceThreshold = 15f; // 沈黙と判断する時間（秒）

    private string apiKey;

    // OpenaiWebAPI への参照を持つ（GPTにメッセージを送るため）
    public OpenaiWebAPI gptApi;

    [System.Serializable]
    public class Body
    {
        public string model;
        public string input;
        public string voice;
    }

    [Serializable]
    public class OpenAIResponse
    {
        public string audio_content;
        public Metadata metadata;
    }

    [Serializable]
    public class Metadata
    {
        public string format;
        public int sample_rate;
        public float duration;
    }

    [System.Serializable]
    public class ApiConfig
    {
        public string API_KEY;
    }

    void Start()
    {
        // APIキーをJSONファイルから読み込む
        LoadApiKeyFromJson();

        if (gptApi != null && gptApi.VoicetoText != null)
        {
            gptApi.VoicetoText.OnVoiceInputDetected += OnVoiceInputReceived;
        }
    }

    //APIキーを読み込む
    private void LoadApiKeyFromJson()
    {
        // ResourcesフォルダからJSONファイルを読み込む
        TextAsset jsonFile = Resources.Load<TextAsset>("api_config");
        if (jsonFile != null)
        {
            ApiConfig config = JsonUtility.FromJson<ApiConfig>(jsonFile.text);
            apiKey = config.API_KEY;
            //Debug.Log("Loaded API Key");
        }
        else
        {
            Debug.LogError("API config file not found");
        }
    }

    private void Update()
    {
        if (isPlayingAudio)
        {
            // 再生中の場合はタイマーをリセット
            silenceTimer = 0f;
            isCheckingSilence = false;
            return; // 再生中なら以降の処理をスキップ
        }

        if (isCheckingSilence)
        {
            // 再生が終了し、沈黙チェック中の場合にタイマーを進める
            silenceTimer += Time.deltaTime;
            if (silenceTimer >= silenceThreshold)
            {
                Debug.Log("silence for 15seconds");
                HandleSilence(); //沈黙続いたらまくしたてる、onにしたければコメントアウト外す
            }
        }
    }

    public void OnVoiceInputReceived()
    {
        Debug.Log("Voice input detected. Stopping silence timer.");
        silenceTimer = 0f; // タイマーをリセット
        isCheckingSilence = false; // 沈黙チェックを無効化
    }

    private void HandleSilence()
    {
        // 沈黙時の処理（デバッグ用）
        Debug.Log("User is silent for too long. Timer value: " + silenceTimer);
        silenceTimer = 0f; // タイマーリセット
        isCheckingSilence = false; // チェック停止

        // GPTに沈黙メッセージを送信する処理を一時的に無効化
        if (gptApi != null)
        {
            Debug.Log("send silent message");
            gptApi.SendSilentMessage();
        }
    }

    //OpenaiWebApiから送られてきたテキスト、これを実行する
    public void ToSpeach(string input)
    {
        StartCoroutine(GetMp3(input));
    }

    private AudioSource audioSource;
    private const bool deleteCachedFile = true;

    private IEnumerator GetMp3(string input)
    {
        var startTime = Time.time;
        Body body = new Body
        {
            model = "tts-1",
            input = input,
            voice = "nova",
        };

        string jsonBody = JsonUtility.ToJson(body);

        //APIのURI、HTTPメソッド、ヘッダーの設定
        string RequestURL = "https://api.openai.com/v1/audio/speech";
        UnityWebRequest request = new UnityWebRequest(RequestURL, "POST");
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        //APIのリクエストを送信
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + request.error);
        }
        else
        {
            // レスポンスの解析
            // var jsonResponse = JsonUtility.FromJson<OpenAIResponse>(request.downloadHandler.text);
            // byte[] audioData = Convert.FromBase64String(jsonResponse.audio_content);

            // オーディオ再生
            ToAudioClip(request.downloadHandler.data);
            var duringTime = Time.time - startTime;
            ProcessingTime.text = duringTime.ToString();
            Debug.Log($"音声処理時間：{duringTime}");
        }
    }

    private void ToAudioClip(byte[] audioData)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "audio.mp3");
        File.WriteAllBytes(filePath, audioData);

        StartCoroutine(PlayAudio(filePath));
    }

    private IEnumerator PlayAudio(string filePath)
    {
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(
            "file://" + filePath,
            AudioType.MPEG
        );
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.clip = audioClip;
            isPlayingAudio = true; //再生中
            isCheckingSilence = false; // 再生中は沈黙チェックを無効化
            audioSource.Play();
            Debug.Log(isPlayingAudio);

            while (audioSource.isPlaying)
            {
                yield return null;
            }
            isPlayingAudio = false; //再生終了
            isCheckingSilence = true; // 沈黙チェック開始
            Debug.Log(isPlayingAudio);
        }
        else
            Debug.LogError("Audio file loading error: " + request.error);

        if (deleteCachedFile)
            File.Delete(filePath);
    }

    public bool IsPlayingAudio()
    {
        return isPlayingAudio;
    }

    void OnDestroy()
    {
        if (gptApi != null && gptApi.VoicetoText != null)
        {
            gptApi.VoicetoText.OnVoiceInputDetected -= OnVoiceInputReceived;
        }
    }
}
