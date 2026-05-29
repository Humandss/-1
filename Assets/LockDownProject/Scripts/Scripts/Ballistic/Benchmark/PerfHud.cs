using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace LockDown.Ballistic.Benchmark
{
    /// <summary>
    /// 실시간 성능 측정 HUD. 화면에 FPS / 프레임타임 / 메인스레드 시간 / GC / 활성 총알을 표시하고,
    /// 키 입력으로 일정 구간을 샘플링해서 평균/최악값을 콘솔에 자동 출력한다.
    /// Profiler 창을 수동으로 안 봐도 수치가 정리되어 나옴.
    ///
    /// ProfilerRecorder API로 "Main Thread" CPU 시간과 "GC Allocated In Frame"을
    /// 직접 읽어서 렌더 비용과 분리된 정확한 측정.
    ///
    /// 사용:
    /// 1. 빈 GameObject에 부착 (씬에 하나)
    /// 2. Play
    /// 3. baseline 측정: 총알 0발 상태에서 [V]
    /// 4. NEW 시스템 발사(G) → 안정화 후 [B]
    /// 5. LEGACY 발사(H) → 안정화 후 [N]
    /// 6. [C] → 세 결과 비교 표 콘솔 출력
    ///
    /// 키:
    ///   [V] = baseline 측정 (총알 0발)
    ///   [B] = NEW 측정
    ///   [N] = LEGACY 측정
    ///   [C] = 비교 출력
    /// </summary>
    public class PerfHud : MonoBehaviour
    {
        [SerializeField, Tooltip("샘플링 지속 시간(초)")]
        private float sampleDuration = 3f;

        [SerializeField] private int fontSize = 18;

        private float smoothDt;

        // ProfilerRecorder
        private ProfilerRecorder mainThreadRecorder;
        private ProfilerRecorder gcAllocRecorder;

        // 샘플링 상태
        private bool sampling;
        private string sampleLabel;
        private float sampleElapsed;
        private int sampleFrames;
        private float sampleDtSum;
        private float sampleDtMax;
        private double sampleMainMsSum;
        private double sampleMainMsMax;
        private long sampleGcSum;

        private struct Result
        {
            public bool valid;
            public string label;
            public float avgFps;
            public float avgMs;
            public float worstMs;
            public double avgMainMs;     // 메인 스레드 평균 (렌더 제외)
            public double worstMainMs;
            public long avgGcPerFrame;   // 프레임당 GC 할당 바이트
            public int activeBullets;
        }
        private Result baseline;
        private Result resNew;
        private Result resLegacy;

        private void OnEnable()
        {
            // "Main Thread" = 메인 스레드 한 프레임 총 CPU 시간 (ns)
            mainThreadRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            // "GC Allocated In Frame" = 프레임당 GC 힙 할당 바이트
            gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void OnDisable()
        {
            mainThreadRecorder.Dispose();
            gcAllocRecorder.Dispose();
        }

        private void Update()
        {
            smoothDt = Mathf.Lerp(smoothDt, Time.unscaledDeltaTime, 0.1f);

            if (Input.GetKeyDown(KeyCode.V)) StartSampling("BASELINE");
            if (Input.GetKeyDown(KeyCode.B)) StartSampling("NEW");
            if (Input.GetKeyDown(KeyCode.N)) StartSampling("LEGACY");
            if (Input.GetKeyDown(KeyCode.C)) PrintComparison();

            if (sampling) TickSampling();
        }

        private void StartSampling(string label)
        {
            sampling = true;
            sampleLabel = label;
            sampleElapsed = 0f;
            sampleFrames = 0;
            sampleDtSum = 0f;
            sampleDtMax = 0f;
            sampleMainMsSum = 0;
            sampleMainMsMax = 0;
            sampleGcSum = 0;
            Debug.Log($"[PerfHud] ▶ 샘플링 시작: {label} ({sampleDuration}s)");
        }

        private void TickSampling()
        {
            float dt = Time.unscaledDeltaTime;
            sampleElapsed += dt;
            sampleFrames++;
            sampleDtSum += dt;
            if (dt > sampleDtMax) sampleDtMax = dt;

            double mainMs = mainThreadRecorder.Valid ? mainThreadRecorder.LastValue * 1e-6 : 0; // ns→ms
            sampleMainMsSum += mainMs;
            if (mainMs > sampleMainMsMax) sampleMainMsMax = mainMs;

            if (gcAllocRecorder.Valid) sampleGcSum += gcAllocRecorder.LastValue;

            if (sampleElapsed >= sampleDuration)
                FinishSampling();
        }

        private void FinishSampling()
        {
            sampling = false;

            float avgDt = sampleFrames > 0 ? sampleDtSum / sampleFrames : 0f;
            var r = new Result
            {
                valid = true,
                label = sampleLabel,
                avgFps = avgDt > 0f ? 1f / avgDt : 0f,
                avgMs = avgDt * 1000f,
                worstMs = sampleDtMax * 1000f,
                avgMainMs = sampleFrames > 0 ? sampleMainMsSum / sampleFrames : 0,
                worstMainMs = sampleMainMsMax,
                avgGcPerFrame = sampleFrames > 0 ? sampleGcSum / sampleFrames : 0,
                activeBullets = GetActiveBulletCount(),
            };

            if (sampleLabel == "BASELINE") baseline = r;
            else if (sampleLabel == "NEW") resNew = r;
            else resLegacy = r;

            var sb = new StringBuilder();
            sb.AppendLine($"===== [PerfHud] {sampleLabel} =====");
            sb.AppendLine($"  활성 총알:    {r.activeBullets}");
            sb.AppendLine($"  평균 FPS:     {r.avgFps:F1}");
            sb.AppendLine($"  평균 프레임:  {r.avgMs:F2} ms  (최악 {r.worstMs:F2})");
            sb.AppendLine($"  메인스레드:   {r.avgMainMs:F2} ms  (최악 {r.worstMainMs:F2})");
            sb.AppendLine($"  GC/프레임:    {r.avgGcPerFrame / 1024f:F1} KB");
            Debug.Log(sb.ToString());
        }

        private void PrintComparison()
        {
            if (!resNew.valid || !resLegacy.valid)
            {
                Debug.LogWarning("[PerfHud] 비교하려면 NEW([B])와 LEGACY([N]) 둘 다 측정 필요");
                return;
            }

            // baseline 빼서 순수 총알 비용 계산 (baseline 있을 때만)
            double newPure = baseline.valid ? resNew.avgMainMs - baseline.avgMainMs : resNew.avgMainMs;
            double legPure = baseline.valid ? resLegacy.avgMainMs - baseline.avgMainMs : resLegacy.avgMainMs;
            double pureRatio = newPure > 0.001 ? legPure / newPure : 0;

            double mainRatio = resNew.avgMainMs > 0.001 ? resLegacy.avgMainMs / resNew.avgMainMs : 0;
            float msRatio = resNew.avgMs > 0.001f ? resLegacy.avgMs / resNew.avgMs : 0;

            // worst 프레임에서 baseline 빼서 순수 FixedUpdate 피크 비용
            double newWorstPure = baseline.valid ? resNew.worstMainMs - baseline.avgMainMs : resNew.worstMainMs;
            double legWorstPure = baseline.valid ? resLegacy.worstMainMs - baseline.avgMainMs : resLegacy.worstMainMs;
            double worstPureRatio = newWorstPure > 0.001 ? legWorstPure / newWorstPure : 0;

            var sb = new StringBuilder();
            sb.AppendLine("================================================");
            sb.AppendLine("            성능 비교: NEW vs LEGACY");
            sb.AppendLine("================================================");
            if (baseline.valid)
                sb.AppendLine($"  [baseline 메인: avg {baseline.avgMainMs:F2} / worst {baseline.worstMainMs:F2} ms, 총알 {baseline.activeBullets}]");
            sb.AppendLine($"  활성 총알:    NEW {resNew.activeBullets}   |  LEGACY {resLegacy.activeBullets}");
            sb.AppendLine($"  평균 FPS:     NEW {resNew.avgFps:F1}   |  LEGACY {resLegacy.avgFps:F1}");
            sb.AppendLine($"  평균 메인:    NEW {resNew.avgMainMs:F2}ms |  LEGACY {resLegacy.avgMainMs:F2}ms  ({mainRatio:F1}x)");
            if (baseline.valid)
                sb.AppendLine($"  ★순수 총알:   NEW {newPure:F2}ms |  LEGACY {legPure:F2}ms  ({pureRatio:F1}x 가벼움)");
            sb.AppendLine("  --- 참고: worst 프레임 (노이즈 많음) ---");
            sb.AppendLine($"  worst 메인:   NEW {resNew.worstMainMs:F2}ms |  LEGACY {resLegacy.worstMainMs:F2}ms");
            sb.AppendLine($"  GC/프레임:    NEW {resNew.avgGcPerFrame / 1024f:F1}KB |  LEGACY {resLegacy.avgGcPerFrame / 1024f:F1}KB");
            sb.AppendLine("================================================");
            Debug.Log(sb.ToString());
        }

        private int GetActiveBulletCount()
        {
            var sys = LockDown.Ballistic.Job.BulletSimulationSystem.Instance;
            int jobActive = sys != null ? sys.Active : 0;
            int legacyActive = LegacyBulletBenchmark.ActiveCount + LegacyBallisticProjectile.ActiveCount;
            return Mathf.Max(jobActive, legacyActive);
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
            };
            style.normal.textColor = Color.white;

            float fps = smoothDt > 0f ? 1f / smoothDt : 0f;
            float ms = smoothDt * 1000f;
            double mainMs = mainThreadRecorder.Valid ? mainThreadRecorder.LastValue * 1e-6 : 0;

            bool burstOn = Unity.Burst.BurstCompiler.Options.EnableBurstCompilation;

            var sb = new StringBuilder();
            sb.AppendLine($"FPS: {fps:F0}   프레임: {ms:F2} ms");
            sb.AppendLine($"메인스레드: {mainMs:F2} ms");
            sb.AppendLine($"활성 총알: {GetActiveBulletCount()}");
            sb.AppendLine($"Burst: {(burstOn ? "ON" : "OFF ⚠️")}");
            sb.AppendLine(sampling
                ? $"● 측정 중... {sampleLabel} ({sampleElapsed:F1}/{sampleDuration:F0}s)"
                : "[V]base [B]NEW [N]LEGACY [C]비교");

            GUI.Box(new Rect(8, 40, 380, 134), "");
            GUI.Label(new Rect(16, 44, 366, 126), sb.ToString(), style);
        }
    }
}
