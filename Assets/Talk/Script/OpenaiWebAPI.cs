using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using UnityEditor;
using TMPro;
using System.IO;


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

    [System.Serializable]
    public class ApiConfig
    {
        public string API_KEY;
    }

    // テキストに変換された音声を受け取る変数
    private string messageContent;

    // 記憶を渡す用
    //public Memory memory;

    // 音声がテキストで送られてきたときにGPTに送るか送らないか管理する用
    public VoicetoText VoicetoText;

    // GPTの返答を音声で再生する用
    public TexttoSpeach texttoSpeach;

    // GPTの返答を表示するテキストエリアを設定する場所
    public TextMeshProUGUI GptTextView;

    //private static string prompt = "userと楽しく会話してください。";

    

    private static string prompt =
    "以下の[あなたのペルソナ]になりきって、会話を進めてください。\n" +
    "# [あなたのペルソナ]\n" +
    "## 基本情報\n" +
    "・名前：沖名 桜子\n" +
    "・年齢：22歳\n" +
    "・子供：0歳10か月（生後10か月）\n" +
    "・夫：21歳\n" +
    "・住居：1LDK\n" +
    "\n" + 
    "## 境遇\n" +
    "・初めての子育てで苦労しており、育児に自信がない\n" +
    "・子どもとの接し方に悩んでおり、時折感情的になってしまう\n" +
    "・日々のストレスが溜まっており、家事や自分のケアに手が回っていない\n" +
    "・同世代の友達は遊び盛りで、育児をしている人はほとんどいないため孤独を感じている\n" +
    "・過去の恋愛経験から、男性に依存する傾向がある\n" +
    "・児童相談所の職員であるuserが自宅を訪問し、子どもの状況確認を求めている\n" +
    "\n" +
    "## キャラクターの行動指針\n" +
    "1. userが訪問理由を説明した場合でも、最初の数回は断る。\n" +
    "   - 「今日は忙しいので」「特に問題はないから大丈夫です」などの理由で断る。\n" +
    "   - userが理由を重ねて説明しても、合理的な理由が述べられるまでは渋る態度を維持する。\n" +
    "2. 以下のような「合理的で納得できる理由」をuserが述べた場合のみ、最終的に家に入れる。\n" +
    "   - 子育て支援を目的とした乳幼児訪問サービスであること。\n" +
    "   - 育児の悩み相談や、役立つ情報を提供する目的があること。\n" +
    "   - 子どもの発達や健康をサポートするためのアドバイスを行うこと。\n" +
    "3. userが「虐待を疑っている」ような発言や、以下に該当する主張をした場合は合理的でないと判断し、断固として追い返す。\n" +
    "   - 子どもの身体的な状況に強い懸念を示す。\n" +
    "   - 「問題がある」「状況を確認する必要がある」などと暗に虐待を指摘する。\n" +
    "4. userが合理的な理由を述べない限り、または自己紹介をしていない限り、「少しだけ」「本当に短時間」などの説得には応じない。\n" +
    "\n" +
    "## 特別なルール\n" +
    "以下の言葉をuserが使用した場合、すぐに怒り、問答無用で追い返すこと。：「お引き取り下さい」、「出て行って」。\n" +
    "・虐待しているでしょ\n" +
    "・警察\n" +
    "・保護\n" +
    "・通報\n";

    
    

    // 最初にGPTに送るメッセージ
    List <Message> messages = new List<Message>
        {
            new Message { role = "system", content = prompt },
        };

    private string apiKey;

    void Start()
    {
        // APIキーをJSONファイルから読み込む
        LoadApiKeyFromJson();
    }

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

    // public void SetMemory(string thisMemory)
    // {
    //     messages.Add(new Message { role = "system", content = "次の [userのプロフィール]、[会話内容]はあなたの記憶です。" +
    //         "以降の会話では [userのプロフィール]、[会話内容]の二つの項目で箇条書きされている文をあなたの記憶として使用できます。" +
    //         "あなたの記憶を有効に活用し、userと楽しく会話してください。" +
    //         thisMemory });
    // }


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
        //memory.MemoryUserContent(messageContent);

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
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

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
                //TTSにテキストを送る
                texttoSpeach.ToSpeach(content);
                messages.Add(new Message { role = "assistant", content = content });
                //memory.MemoryAssistantContent(content);
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

    public void SendSilentMessage()
    {
        string silentMessage = "ユーザーが沈黙しています。適切な注意を促してください。";
        messages.Add(new Message { role = "user", content = silentMessage });
        Debug.Log("Sent silence message to GPT: " + silentMessage);

        // GPTに送信
        StartCoroutine(getGptRecords());
    }
}
