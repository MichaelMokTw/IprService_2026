using NLog;
using shpa3api;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace richpod.synway;

public static class LibSynwayEx {
    private static readonly Logger Logger = LogManager.GetLogger("SynCTIError");
    public static bool IsLoggingEnabled { get; set; } = true;

    #region 核心日誌引擎

    // 針對一般函式的模板
    private static T Invoke<T>(Func<T> apiCall, string args, [CallerMemberName] string methodName = "") {
        LogCall(methodName, args);
        try {
            T result = apiCall();
            LogReturn(methodName, result?.ToString() ?? "null");
            if (result is int iRet && iRet < 0) HandleError(methodName, iRet);
            return result;
        }
        catch (Exception ex) {
            LogException(methodName, ex);
            throw;
        }
    }

    // 拆解出的日誌工具，供 ref 函式手動呼叫
    private static void LogCall(string method, string args) {
        if (IsLoggingEnabled) Logger.Trace($"[CALL] {method}({args})");
    }

    private static void LogReturn(string method, string ret) {
        if (IsLoggingEnabled) Logger.Trace($"[RET] {method} -> {ret}");
    }

    private static void HandleError(string method, int code) {
        string errMsg = GetLastErrMsg();
        Logger.Error($"[ERROR] {method} failed (Code: {code}). Msg: {errMsg}");
    }

    private static void LogException(string method, Exception ex) {
        Logger.Fatal(ex, $"[EXCEPTION] {method} crashed!");
    }

    private static string GetLastErrMsg() {
        var sb = new StringBuilder(256);
        try { SsmApi.SsmGetLastErrMsg(sb); return sb.ToString().Trim(); }
        catch { return "GetLastErrMsg Failed"; }
    }

    #endregion

    #region 初始化與系統 API (修正 ref 問題)

    public static int SsmStartCtiEx(string configFile, string indexFile, ref EVENT_SET_INFO_CALLBACKA callback, out string errMsg) {
        errMsg = string.Empty;
        if (!File.Exists(configFile) || !File.Exists(indexFile)) return -3;

        string methodName = nameof(SsmStartCtiEx);
        LogCall(methodName, $"cfg:{configFile}");

        // 解決 CS1628: 針對 ref 參數手動呼叫 API
        int ret = SsmApi.SsmStartCtiEx(configFile, indexFile, true, ref callback);

        LogReturn(methodName, ret.ToString());
        if (ret < 0) {
            errMsg = GetLastErrMsg();
            HandleError(methodName, ret);
        }
        return ret;
    }

    public static int SsmStartCti(string configFile, string indexFile, out string errMsg) {
        errMsg = string.Empty;
        if (!File.Exists(configFile) || !File.Exists(indexFile)) return -3;
        int ret = Invoke(() => SsmApi.SsmStartCti(configFile, indexFile), $"cfg:{configFile}");
        if (ret < 0) errMsg = GetLastErrMsg();
        return ret;
    }

    public static int CloseCti() => Invoke(() => SsmApi.SsmCloseCti(), "");
    public static int SetInterEventType(int type) => Invoke(() => SsmApi.SsmSetInterEventType(type), $"type:{type}");
    public static int GetInterEventType() => Invoke(() => SsmApi.SsmGetInterEventType(), "");

    #endregion

    #region 硬體與版本資訊 (修正 ref 問題)

    public static int GetDllVersion(ref SSM_VERSION ver) {
        string methodName = nameof(GetDllVersion);
        LogCall(methodName, "");

        // 直接調用 API，避免 Lambda 捕獲 ref
        int ret = SsmApi.SsmGetDllVersion(ref ver);

        LogReturn(methodName, ret.ToString());
        if (ret < 0) HandleError(methodName, ret);
        return ret;
    }

    public static int GetMaxCfgBoard() => Invoke(() => SsmApi.SsmGetMaxCfgBoard(), "");
    public static int GetMaxUsableBoard() => Invoke(() => SsmApi.SsmGetMaxUsableBoard(), "");
    public static int GetMaxCh() => Invoke(() => SsmApi.SsmGetMaxCh(), "");
    public static string GetDllVerStr(SSM_VERSION v) => $"{v.ucMajor}.{v.ucMinor}.{v.usInternal}.{v.usBuild}";
    public static int GetBoardAccreditId(int bId) => Invoke(() => SsmApi.SsmGetAccreditId(bId), $"bId:{bId}");
    public static int GetAccreditIdEx(int bId) => Invoke(() => SsmApi.SsmGetAccreditIdEx(bId), $"bId:{bId}");
    public static int GetBoardModel(int bId) => Invoke(() => SsmApi.SsmGetBoardModel(bId), $"bId:{bId}");
    public static int GetPciSerialNo(int bId) => (int)Invoke(() => SsmApi.SsmGetPciSerialNo(bId), $"bId:{bId}");

    #endregion

    #region IPR (IP Recording) API (修正所有 ref 問題)

    public static int GetIPRRecSlaverCount(int bId) => Invoke(() => SsmApi.SsmIPRGetRecSlaverCount(bId), $"bId:{bId}");
    public static int GetIPRMonitorType(int bId) => Invoke(() => SsmApi.SsmIPRGetMonitorType(bId), $"bId:{bId}");

    public static int IPRAddStationToMap(int bId, int sId, string addr, int port)
        => Invoke(() => SsmApi.SsmIPRAddStationToMap(bId, sId, addr, port), $"bId:{bId}, sId:{sId}, addr:{addr}:{port}");

    public static int IPRGetRecSlaverList(int bId, int num, ref int retNum, ref IPR_SLAVERADDR addr) {
        string methodName = nameof(IPRGetRecSlaverList);
        LogCall(methodName, $"bId:{bId}, num:{num}");
        int ret = SsmApi.SsmIPRGetRecSlaverList(bId, num, ref retNum, ref addr);
        LogReturn(methodName, ret.ToString());
        if (ret < 0) HandleError(methodName, ret);
        return ret;
    }

    public static int IPRStartRecSlaver(int bId, int sId, ref int res, ref int pairs) {
        string methodName = nameof(IPRStartRecSlaver);
        LogCall(methodName, $"bId:{bId}, sId:{sId}");
        int ret = SsmApi.SsmIPRStartRecSlaver(bId, sId, ref res, ref pairs);
        LogReturn(methodName, ret.ToString());
        if (ret < 0) HandleError(methodName, ret);
        return ret;
    }

    public static int IPRActiveSession(int ch, int sId, uint sessId, StringBuilder pA, int pP, ref int pRP, int pC, StringBuilder sA, int sP, ref int sRP, int sC) {
        string methodName = nameof(IPRActiveSession);
        LogCall(methodName, $"ch:{ch}, sess:{sessId}");
        int ret = SsmApi.SsmIPRActiveSession(ch, sId, sessId, pA, pP, ref pRP, pC, sA, sP, ref sRP, sC);
        LogReturn(methodName, ret.ToString());
        if (ret < 0) HandleError(methodName, ret);
        return ret;
    }

    public static int IPRDeActiveSession(int ch) => Invoke(() => SsmApi.SsmIPRDeActiveSession(ch), $"ch:{ch}");
    public static int IPRSendSession(int ch, string pA, int pP, string sA, int sP)
        => Invoke(() => SsmApi.SsmIPRSendSession(ch, pA, pP, sA, sP), $"ch:{ch}");

    public static string GetIPAddress(IPR_Addr a) {
        var b = a.s_ip.addr.S_un_b;
        return $"{b.s_b1}.{b.s_b2}.{b.s_b3}.{b.s_b4}:{a.s_ip.usPort}";
    }

    #endregion

    #region 錄音與事件 API

    public static int RecToFileA(int ch, string file, int codec, LPRECTOMEM pfn)
        => Invoke(() => SsmApi.SsmRecToFileA(ch, file, codec, 0, 0, 0xFFFFFFFF - 1, 1, pfn), $"ch:{ch}, file:{file}");

    public static int StopRecToFile(int ch, out string errMsg) {
        errMsg = string.Empty;
        int ret = Invoke(() => SsmApi.SsmStopRecToFile(ch), $"ch:{ch}");
        if (ret < 0) errMsg = GetLastErrMsg();
        return ret;
    }

    public static int PauseRecToFile(int ch) => Invoke(() => SsmApi.SsmPauseRecToFile(ch), $"ch:{ch}");
    public static int RestartRecToFile(int ch) => Invoke(() => SsmApi.SsmRestartRecToFile(ch), $"ch:{ch}");

    public static int SetEvent(ushort ev, int refer, int output, ref EVENT_SET_INFO set) {
        string methodName = nameof(SetEvent);
        LogCall(methodName, $"ev:{ev}, ref:{refer}");
        int ret = SsmApi.SsmSetEvent(ev, refer, output, ref set);
        LogReturn(methodName, ret.ToString());
        if (ret < 0) HandleError(methodName, ret);
        return ret;
    }

    #endregion
}