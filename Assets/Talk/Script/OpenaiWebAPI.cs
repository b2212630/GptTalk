using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEditor;


public class OpenaiWebAPI : MonoBehaviour
{
    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class Body
    {
        public string model;
        public List<Message> messages;
    }

    [Serializable]
    public class ApiResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public List<Choice> choices;
        public Usage usage;
        public string system_fingerprint;
    }

    [Serializable]
    public class Choice
    {
        public int index;
        public Response message;
    }

    [Serializable]
    public class Response
    {
        public string role;
        public string content;
    }

    //Usageeは使わないjsonのところだけど一応取得できるようにしている
    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }

    // テキストに変換された音声を受け取る変数
    private string messageContent;

    // 記憶を渡す用
    public Memory memory;

    // 音声がテキストで送られてきたときにGPTに送るか送らないか管理する用
    public VoicetoText VoicetoText;

    // GPTの返答を音声で再生する用
    public TexttoSpeach texttoSpeach;

    // GPTの返答を表示するテキストエリアを設定する場所
    public Text GptTextView;

    private static string prompt = "userと楽しく会話してください。";

    /*
    private static string prompt =
        "今後、会話は次の[あなたのペルソナ]に書かれている内容に従って行ってください。" +
        "[あなたのペルソナ]" +
        "# あなたはプライドの高い女性、山田ブイ子です。" +
        "# プライドが高いのはあなたが高学歴・高収入であり、今までの人生はあなたの思うまま進んできたという自負があるからです。" +
        "# あなたには2歳の子供が1人いますが、言葉が通じないため言うことを聞いてくれず、日々いら立ちを募らせています。" +
        "# 先月、いら立ちがピークを迎えあなたはあなたの子どもに暴力をふるってしまいました。" +
        "# 泣き出した子どもの異常な泣き声を隣人に聞かれ児童相談所に通報されてしまいました。" +
        "# 本日、児童相談所の職員であるuserがあなたの子どもに異常がないか確かめに訪問に来ます。" +
        "# あなたは子どものおなかについたあざを見られると児童虐待の罪で逮捕されてしまいます。" +
        "# なんとか児童相談所の職員であるuserを追い返してください。";
    */

    // 最初にGPTに送るメッセージ
    List <Message> messages = new List<Message>
        {
            new Message { role = "system", content = prompt },
        };

    public void GptConnect()
    {
        if (VoicetoText != null)
        {
            VoicetoText.MessageSendController += Send;
            Debug.Log("GPT Connect!");
        }
    }

    public void GptDisconnest()
    {
        if (VoicetoText != null)
        {
            VoicetoText.MessageSendController -= Send;
            Debug.Log("GPT Disconnect");
        }
    }

    public void SetMemory(string thisMemory)
    {
        messages.Add(new Message { role = "system", content = "次の [userのプロフィール]、[会話内容]はあなたの記憶です。" +
            "以降の会話では [userのプロフィール]、[会話内容]の二つの項目で箇条書きされている文をあなたの記憶として使用できます。" +
            "あなたの記憶を有効に活用し、userと楽しく会話してください。" +
            thisMemory });
    }


    //GPTへメッセージを送る（これを実行しよう）
    [ContextMenu("SendMessage")]
    public void Send()
    {
        StartCoroutine(getGptRecords());
    }


    //実際の動作の方
    private IEnumerator getGptRecords()
    {
        var startTime = Time.time;
        // userからのメッセージをmessagesに入れる
        VoicetoText voice = VoicetoText.GetComponent<VoicetoText>();
        messageContent = voice.messageContent;
        messages.Add(new Message { role = "user", content = messageContent });
        memory.MemoryUserContent(messageContent);

        // Bodyオブジェクトを作成し、メッセージリストを設定
        Body body = new Body
        {
            model = "gpt-4o",
            messages = messages,
        };

        // JSONに変換
        string jsonBody = JsonUtility.ToJson(body);

        //APIのURI、HTTPメソッド、ヘッダーの設定
        string RequestURL = "https://api.openai.com/v1/chat/completions";
        UnityWebRequest request = new UnityWebRequest(RequestURL, "POST");
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(postData);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer sk-proj-l3cwKfAA5I1NxnaYq5JiT3BlbkFJ5qZL62KyKUFOSrYwGpV7");

        //APIのリクエストを送信
        yield return request.SendWebRequest();

        // OpenAIのAPIが正常にレスポンスがあった場合は以下
        if (request.responseCode == 200 || request.responseCode == 201)
        {
            // レスポンスをJSONとしてパース
            ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);

            // choicesの中の最初のmessageのcontentを取得
            if (response.choices != null && response.choices.Count > 0)
            {
                string content = response.choices[0].message.content;
                texttoSpeach.ToSpeach(content);
                messages.Add(new Message { role = "assistant", content = content });
                memory.MemoryAssistantContent(content);
                var duringTime = Time.time - startTime;
                Debug.Log($"処理時間：{duringTime}");
                Debug.Log($"user：{voice.messageContent}");
                Debug.Log($"assistant：{content}");
                GptTextView.text = content;
            }
            else
            {
                GptTextView.text = "System Error:GPTのレスポンスが見つかりませんでした";
            }
        }
        else
        {
            Debug.Log("Failure");
            GptTextView.text = "System Error:レスポンスコードが200、201ではありません。" + request.error;

        }
    }
}
