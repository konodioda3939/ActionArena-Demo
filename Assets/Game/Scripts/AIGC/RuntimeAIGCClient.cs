using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ActionArena.AIGC
{
    /// <summary>
    /// 运行时 AIGC 客户端：HTTP 调本地 FastAPI 推理服务（默认 http://127.0.0.1:8000）。
    /// 纯 UnityEngine.Networking，无 UnityEditor 依赖，打包后可用。
    ///
    /// 接口契约（见 D:\aigc-project\inference_server\main.py）：
    ///   POST /generate  JSON {prompt, negative_prompt, steps, guidance_scale, seed, fast_mode, model} → image/png
    ///   GET  /health    → JSON {status:"ready"|"loading"...}
    ///   GET  /models    → JSON {models:[{key,label,...}], default}
    /// </summary>
    public static class RuntimeAIGCClient
    {
        public const string DefaultBaseUrl = "http://127.0.0.1:8000";

        const string DefaultNegative =
            "lowres, bad anatomy, text, error, worst quality, low quality, blurry, watermark";

        /// <summary>文字生图。fastMode=true → LCM ~0.75s/张（运行时演示首选）。
        /// model: anime / realistic / texture（不同模型按需切换，首次用新模型服务端会下载）。</summary>
        public static async Task<Texture2D> GenerateImage(
            string baseUrl, string prompt, bool fastMode, int? seed = null, string model = "anime")
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = DefaultBaseUrl;

            // 注意：服务端校验 steps>=10。fast_mode=true 时发默认值 25，服务端检测到「未改默认」
            // 会自动改用 LCM 的 6 步 / 1.5 cfg（见 main.py 的 fast_mode 分支）。直接发 6 会被 422 拒绝。
            int steps = 25;
            float cfg = 7.5f;
            string seedJson = seed.HasValue ? seed.Value.ToString() : "null";

            string json =
                "{" +
                $"\"prompt\":{Escape(prompt)}," +
                $"\"negative_prompt\":{Escape(DefaultNegative)}," +
                $"\"steps\":{steps}," +
                $"\"guidance_scale\":{cfg.ToString("F1", CultureInfo.InvariantCulture)}," +
                $"\"seed\":{seedJson}," +
                $"\"fast_mode\":{(fastMode ? "true" : "false")}," +
                $"\"model\":{Escape(model)}" +
                "}";

            string url = baseUrl.TrimEnd('/') + "/generate";
            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 120;

                await SendAsync(req);

                if (req.result != UnityWebRequest.Result.Success)
                    throw new Exception(
                        $"生成失败：{req.error}\n{req.downloadHandler?.text}\n" +
                        $"请确认推理服务已启动（{baseUrl}/docs）。");

                byte[] data = req.downloadHandler.data;
                if (data == null || data.Length == 0)
                    throw new Exception("服务返回空数据。");

                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(data))
                    throw new Exception("返回的不是有效图片。");
                tex.name = "AIGC_" + (req.GetResponseHeader("X-Seed") ?? "skin");
                return tex;
            }
        }

        /// <summary>服务是否就绪（status=="ready"）。</summary>
        public static async Task<bool> CheckHealth(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = DefaultBaseUrl;
            string url = baseUrl.TrimEnd('/') + "/health";
            try
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    req.timeout = 5;
                    await SendAsync(req);
                    if (req.result != UnityWebRequest.Result.Success) return false;
                    return req.downloadHandler.text.Contains("\"ready\"");
                }
            }
            catch
            {
                return false;
            }
        }

        // ===== 内部 =====

        private static Task SendAsync(UnityWebRequest req)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op = req.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);
            return tcs.Task;
        }

        private static string Escape(string s)
        {
            var sb = new StringBuilder((s ?? "").Length + 2);
            sb.Append('"');
            foreach (char c in s ?? "")
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:   sb.Append(c);      break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }
    }
}
