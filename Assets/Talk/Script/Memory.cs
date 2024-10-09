// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Networking;
// using UnityEngine.UI;
// using System;
// using UnityEditor;


// public class Memory : MonoBehaviour
// {
//     [System.Serializable]
//     public class Message
//     {
//         public string role;
//         public string content;
//     }

//     [System.Serializable]
//     public class Body
//     {
//         public string model;
//         public List<Message> messages;
//     }

//     [Serializable]
//     public class ApiResponse
//     {
//         public string id;
//         public string @object;
//         public long created;
//         public string model;
//         public List<Choice> choices;
//         public Usage usage;
//         public string system_fingerprint;
//     }

//     [Serializable]
//     public class Choice
//     {
//         public int index;
//         public Response message;
//     }

//     [Serializable]
//     public class Response
//     {
//         public string role;
//         public string content;
//     }

//     //Usageeは使わないjsonのところだけど一応取得できるようにしている
//     [Serializable]
//     public class Usage
//     {
//         public int prompt_tokens;
//         public int completion_tokens;
//         public int total_tokens;
//     }

//     // 記憶を渡す用
//     public OpenaiWebAPI openaiApi;
//     private string memoryContent;

//     // GPTの返答を表示するテキストエリアを設定する場所
//     public Text MemoryTextView;

//     private static string prompt =
//         "あなたは、あなたとuserの会話を要約する仕事をしています。" +
//         "過去のあなたとuserの会話履歴から要約を行ってください。" +
//         "会話の要約をするときは、以下のルールに従って要約を行って下さい。" +
//         "# [userのプロフィール]、[会話内容]という2つの項目に分けて要約を行ってください。" +
//         "# [userのプロフィール]、[会話内容]という項目ではそれぞれ箇条書きで要約を行ってください。" +
//         "# [userのプロフィール]という項目では、会話からわかるuserの情報を箇条書きでまとめてください。（例：・userはハンバーグが好き。 ・userはかつやでアルバイトをしている。）" +
//         "# [会話内容]という項目では、会話の話題ごとに簡潔に要約した内容を箇条書きでまとめてください。（例：・userは勉強が楽しいと感じている。 ・明日の天気について議論し、晴れだと嬉しいという結論になった。）";

//     // 最初にGPTに送るメッセージ
//     List<Message> messages = new List<Message>
//         {
//             new Message { role = "system", content = prompt },
//         };

//     public void MemoryUserContent(string memoryContent)
//     {
//         messages.Add(new Message { role = "user", content = memoryContent });
//     }
//     public void MemoryAssistantContent(string memoryContent)
//     {
//         messages.Add(new Message { role = "assistant", content = memoryContent });
//     }

//     //GPTへメッセージを送る（これを実行しよう）
//     [ContextMenu("SendMemory")]
//     public void Send()
//     {
//         StartCoroutine(getGptRecords());
//     }


//     //実際の動作の方
//     private IEnumerator getGptRecords()
//     {
//         var startTime = Time.time;

//         // Bodyオブジェクトを作成し、メッセージリストを設定
//         Body body = new Body
//         {
//             model = "gpt-4o",
//             messages = messages,
//         };

//         // JSONに変換
//         string jsonBody = JsonUtility.ToJson(body);

//         //APIのURI、HTTPメソッド、ヘッダーの設定
//         string RequestURL = "https://api.openai.com/v1/chat/completions";
//         UnityWebRequest request = new UnityWebRequest(RequestURL, "POST");
//         byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonBody);
//         request.uploadHandler = new UploadHandlerRaw(postData);
//         request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
//         request.SetRequestHeader("Content-Type", "application/json");
//         request.SetRequestHeader("Authorization", "Bearer sk-proj-l3cwKfAA5I1NxnaYq5JiT3BlbkFJ5qZL62KyKUFOSrYwGpV7");

//         //APIのリクエストを送信
//         yield return request.SendWebRequest();

//         // OpenAIのAPIが正常にレスポンスがあった場合は以下
//         if (request.responseCode == 200 || request.responseCode == 201)
//         {
//             // レスポンスをJSONとしてパース
//             ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);

//             // choicesの中の最初のmessageのcontentを取得
//             if (response.choices != null && response.choices.Count > 0)
//             {
//                 string content = response.choices[0].message.content;
//                 messages.Add(new Message { role = "assistant", content = content });
//                 var duringTime = Time.time - startTime;
//                 Debug.Log($"処理時間：{duringTime}");
//                 Debug.Log($"Memory : {content}");
//                 openaiApi.SetMemory(content);
//                 MemoryTextView.text = content;
//             }
//             else
//             {
//                 MemoryTextView.text = "System Error:GPTのレスポンスが見つかりませんでした";
//             }
//         }
//         else
//         {
//             Debug.Log("Failure");
//             MemoryTextView.text = "System Error:レスポンスコードが200、201ではありません。" + request.error;

//         }
//     }
// }
