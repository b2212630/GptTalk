using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using TMPro;


/*
 * 注意
 * ・WebGL形式での出力不可
 * ・「あーーーーーーーーー」という音声を発音しても、「あー」に省略される。
 * 　（伸ばし棒は省略される）
 * 
 * 設定(windows11)
 * ・機能を使用するには
 *   設定→プライバシーとセキュリティ→音声認識→オンライン音声認識　をオンにする
 * 
 */

public class VoicetoText : MonoBehaviour
{
    public TextMeshProUGUI text_;
    public DictationRecognizer m_DictationRecognizer;
    private string content;
    public string messageContent;

    public delegate void TextChangedEventHandler();
    public event TextChangedEventHandler MessageSendController;

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

        // 発音終了時
        //DictationResultのイベントを登録
        // m_DictationRecognizer.DictationResult += (text, confidence) =>
        // {
        //     //音声認識した文章はtextで受け取れます。
        //     text_.text = text;
        //     //GPTに渡す用の変数に格納
        //     messageContent = text;
        //     if (MessageSendController != null)
        //     {
        //         MessageSendController();
        //     }
        // };


        /*
        // 発音中
        m_DictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;
        */

        // 音声入力停止時に再起動
        m_DictationRecognizer.DictationComplete += (completionCause) =>
        {
            if (completionCause == DictationCompletionCause.TimeoutExceeded || completionCause == DictationCompletionCause.Complete)
            {
                //音声認識を起動。
                //m_DictationRecognizer.Start();
            }
            else
            {
                Debug.LogError(completionCause);
            }
        };

        //Dictationを開始
        //m_DictationRecognizer.Start();
    }
    /*
    private void DictationRecognizer_DictationHypothesis(string text)
    {
        text_.text = text;
    }
    */

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

            // 発音終了時の結果を処理
            m_DictationRecognizer.DictationResult += (text, confidence) =>
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
            m_DictationRecognizer.Stop();
        }
    }

}