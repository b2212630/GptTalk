using Azure.Data.Tables;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class AzureTableStorage : MonoBehaviour
{
    private string connectionString = "DefaultEndpointsProtocol=https;AccountName=b2212630;AccountKey=fyAyRnpakFMkow6847RSM8qd1xgVFj0LiEEZLOFUPZoOzYNOkAiF60ndMr7ER2lASAb93Ics5hnp+AStHu6+Yw==;EndpointSuffix=core.windows.net"; // Azure接続文字列
    private string tableName = "DialogueGpt4o"; // 作成済みのテーブル名
    private TableServiceClient tableServiceClient;

    [SerializeField] private TextMeshProUGUI userText; // ユーザーのTextMeshProUGUI
    [SerializeField] private TextMeshProUGUI gptText;  // GPTのTextMeshProUGUI
    [SerializeField] private TextMeshProUGUI processingTimeText;

    private string lastUserText = ""; // 前回保存したユーザーのテキスト
    private string lastGptText = "";  // 前回保存したGPTのテキスト
    private string lastProcessingTime = "";

    // 更新を監視する間隔（秒）
    private float checkInterval = 0.5f;

    // 初期化
    void Start()
    {
        tableServiceClient = new TableServiceClient(connectionString);
        // テキスト変更を監視するコルーチンを開始
        StartCoroutine(CheckTextChanges());
    }

    // テキストの変更を定期的に監視するコルーチン
    private System.Collections.IEnumerator CheckTextChanges()
    {
        while (true)
        {
            if (processingTimeText.text != lastProcessingTime)
            {
                //lastUserText = userText.text;
                //lastGptText = gptText.text;
                lastProcessingTime = processingTimeText.text;

                // データを保存
                SaveDialogueToAzureAsync(userText.text, gptText.text, lastProcessingTime);
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    // データをAzureに保存する
    public async Task SaveDialogueToAzureAsync(string userText, string gptText, string processingTime)
    {
        try
        {
            var tableClient = tableServiceClient.GetTableClient(tableName);

            if (!string.IsNullOrWhiteSpace(userText) && !string.IsNullOrWhiteSpace(gptText) && !string.IsNullOrWhiteSpace(processingTime))
            {
                // エンティティに処理時間を追加
                var dialogueEntity = new DialogueEntity
                {
                    PartitionKey = "Dialogue",
                    RowKey = Guid.NewGuid().ToString(),
                    UserMessage = userText,
                    GPTMessage = gptText,
                    ProcessingTime = processingTime,
                    CurrentTime = DateTime.UtcNow.ToString("o"), // ISO 8601形式で保存
                    Timestamp = DateTime.UtcNow
                };

                await tableClient.AddEntityAsync(dialogueEntity);
                Debug.Log("データが正常に保存されました！");
            }
            else
            {
                Debug.LogWarning("データが空白です。保存できません。");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"データ保存中にエラーが発生しました: {ex.Message}");
        }
    }
}

// Azure Table Storage用のエンティティクラス
public class DialogueEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public Azure.ETag ETag { get; set; }

    public string UserMessage { get; set; } // ユーザーのメッセージ
    public string GPTMessage { get; set; }  // GPTの応答
    public string ProcessingTime { get; set; } // 処理時間
    public string CurrentTime { get; set; } // 現在時刻
}
