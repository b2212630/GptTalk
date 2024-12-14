using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using TMPro;

public class VoicetoText : MonoBehaviour
{
    public TextMeshProUGUI text_;
    public DictationRecognizer m_DictationRecognizer;
    private string content;
    public string messageContent;

    public delegate void TextChangedEventHandler();
    public event TextChangedEventHandler MessageSendController;

    private bool isDictationActive = false; // 音声認識がアクティブかを管理
    


    // オブジェクトが破棄されるとき
    private void OnDestroy()
    {
        // 破棄
        // 下記を記載しないと処理中断時にエラーになる
        m_DictationRecognizer.Stop();
        m_DictationRecognizer.Dispose();
    }

    void Start()
    {
        text_.text = "";

        m_DictationRecognizer = new DictationRecognizer();

        // 発音終了時の結果を処理
        m_DictationRecognizer.DictationResult += (text, confidence) => // ここは一度だけ登録
        {
            // 音声認識した文章をテキストに表示
            text_.text = text;
            // GPTに渡す用の変数に格納
            messageContent = text;
            // MessageSendControllerが設定されていれば実行
            if (MessageSendController != null)
            {
                MessageSendController();
            }
        };

        // 音声入力停止時に再起動
        m_DictationRecognizer.DictationComplete += (completionCause) =>
        {
            if (completionCause == DictationCompletionCause.TimeoutExceeded || completionCause == DictationCompletionCause.Complete)
            {
                //音声認識を起動。
                Debug.Log($"DictationComplete triggered with cause: {completionCause}");

                // フラグが有効な場合のみ再起動
                if (isDictationActive)
                {
                    m_DictationRecognizer.Start();
                    Debug.Log("Dictation Restart");
                }
                
            }
            else
            {
                Debug.LogError(completionCause);
            }
        };
    }

    // OnToggledで音声認識を開始
    public void StartDictation()
    {
        // PhraseRecognitionSystemが動作しているか確認し、停止する
        if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Running)
        {
            Debug.Log("Stopping PhraseRecognitionSystem...");
            PhraseRecognitionSystem.Shutdown();
        }

        // DictationRecognizerが既に動作していない場合のみ開始
        if (m_DictationRecognizer.Status != SpeechSystemStatus.Running)
        {
            Debug.Log("Starting Dictation Recognizer...");
            isDictationActive = true; // フラグを有効化
            m_DictationRecognizer.Start();
        }
    }

    // OnUntoggledで音声認識を停止
    public void StopDictation()
    {
        // DictationRecognizerが動作中の場合のみ停止
        if (m_DictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            Debug.Log("Stopping Dictation Recognizer...");
            isDictationActive = false; // フラグを無効化
            m_DictationRecognizer.Stop();
        }
    }
}
