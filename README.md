# GptTalk
私が卒論の時までに構築したAI対話システムです。
MRをインターフェースにした音声ベースの対話を行うことができます。
## 概要
HoloLens2で動かすことを想定しています。AIにはOpenAIのGPT-4oとGPT-3.5-turboを主に使用しています。
mainはアバターなしのもの、devでは実際に研究の検証で使用したアバターありのものがあります。アバターにはリップシンクを付与しています。
今後は表情やジェスチャーも付与していく予定です。
## 使用技術
- 音声認識：Windows Dictation Recognizer
- 返答生成：OpenAI API
- Text to speech：OpenAI Text-to-speech
- 対話内容の保存：Azure Table Storage
- MRTK3


## 動作環境
Unity 2022.3.28f
 
