using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero.DFS.dfs0;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero;
using System.IO;

using bjd_model.CatchMent;
using bjd_model.Const_Global;
using bjd_model.Mike11;
using bjd_model;
using Baidu.Aip.Speech;

namespace bjd_model.Common
{
    public class Speech
    {
        private readonly Asr _asrClient;
        private readonly Tts _ttsClient;

        public Speech()
        {
            _asrClient = new Asr(Mysql_GlobalVar.now_baidu_APIKey, Mysql_GlobalVar.now_baidu_SecretKey);
            _ttsClient = new Tts(Mysql_GlobalVar.now_baidu_APIKey, Mysql_GlobalVar.now_baidu_SecretKey);
        }

        // 识别本地文件
        public string Get_String_FromLocal(string filepath)
        {
            var data = File.ReadAllBytes(filepath);
            var result = _asrClient.Recognize(data, "pcm", 16000);
            return result.ToString();
        }

        // 识别URL中的语音文件
        public string Get_String_FromUrl(string url)
        {
            var result = _asrClient.Recoginze(
                url, 
                "http://xxx.com/识别结果回调地址", 
                "pcm", 
                16000);
            return result.ToString();
        }

        // 语音合成(mp3)
        public void Get_Sound_FromString(string content_str,string filename)
        {
            // 可选参数
            var option = new Dictionary<string, object>()
            {
                {"spd", 5}, // 语速一般，0-9
                {"vol", 7}, // 音量,0-9
                {"pit", 5}, // 音调,0-9
                {"per", 1}  // 发音人,0为女声，1为男生，3为情感合成（度逍遥），4为情感合成（度丫丫）
            };
            var result = _ttsClient.Synthesis(content_str, option);

            if (result.Success) 
            {
                File.WriteAllBytes(filename, result.Data);
            }
        }
    }
}