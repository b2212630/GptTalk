using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Net;
using System.IO;

public class TexttoSpeach : MonoBehaviour
{
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
            model = "tts-1-hd",
            input = input,
            voice = "nova"
        };

        string jsonBody = JsonUtility.ToJson(body);

        //APIのURI、HTTPメソッド、ヘッダーの設定
        string RequestURL = "https://api.openai.com/v1/audio/speech";
        UnityWebRequest request = new UnityWebRequest(RequestURL, "POST");
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer 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");

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
        using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
            audioSource = gameObject.GetComponent<AudioSource>();
            audioSource.clip = audioClip;
            audioSource.Play();
        }
        else Debug.LogError("Audio file loading error: " + request.error);

        if (deleteCachedFile) File.Delete(filePath);
    }
}
