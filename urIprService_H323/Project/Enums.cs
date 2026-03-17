using System.ComponentModel;

//TDOD: 整理沒用到的刪除
//TDOD: 改 namesapce => Project.Enums

namespace MyProject.ProjectCtrl {

    public enum ENUM_RecProto {        
        Sip = 1,        
        H323 = 2
    }

    public enum ENUM_ChannelStatus {
        [Description("閒置")]
        Idle = 2,

        [Description("響鈴")]
        Ringing = 3,

        [Description("來電")]
        Inbound = 4,

        [Description("撥出")]
        Outbound = 5,
    }

    public enum ENUM_NotifyRecStatus {
        [Description("錄音檔建立失敗")]
        RecFileError = -1,

        [Description("開始錄音")]
        StartRcording = 0,

        [Description("錄音完成")]
        CompleteRecording = 1,
    }

    public enum ENUM_BoardType {
        [Description("類比")]
        Analog = 'a',

        [Description("數位")]
        Digital = 'd',

        [Description("E1/T1")]
        T1E1 = 't',

        [Description("SYNIPR")]
        SynIpr = 'i'
    };

    public enum ENUM_ChannelType {
        [Description("Analog")]
        Analog = 1,

        [Description("Digital")]
        Digital = 2,

        [Description("ASMDR")]
        ASMDR = 3,

        [Description("Trunk")]
        Trunk = 4,

        [Description("SYNIPR")]
        SYNIPR = 5,

        //------ 以下兩個 urcWeb 沒有支援，後續要加 ----------------
        [Description("RawIP")]
        RawIP = 6,
        [Description("CallinCallout")] // 特別為了 Callin/CallOut定義的
        CINCOUT = 7
        //------------------------------------------------------
    };

    public enum ENUM_AddSilenceMethod {
        None = 0,
        ByCapTime = 1,
        ByTimeStamp = 2,        
    }


    public enum ENUM_RawType {        
        UNKNOWN = 0,
        Early_RX = 1,
        TX = 2,        
        RX = 3,        
    }

    public enum ENUM_HoldStatus {
        [Description("Hold 取消")]
        None = 0,

        [Description("按下 Hold")]
        PressToHold = 1,

        [Description("對方 Hold")]
        Beheld = 2,        
    }

    public enum ENUM_RTP_RecFlag {
        [Description("停止錄音")]
        StopRec = -1,   // 停止錄音

        [Description("開始錄音")]
        StartRec = 0,   // 開始錄音        

        [Description("錄音中")]
        Recording = 1,   // 錄音中

        [Description("開始按下 hold")]
        StartPressToHold = 2, // 開始按下 hold(主動)

        [Description("開始被Hold")]
        StartBeHeld = 3, // 開始被Hold(被動)

        [Description("取消 Hold")]
        StopHeld = 4, // 取消 Hold(主被動通用)

        [Description("DTMF按鍵")]
        DTMF = 5, // 按下 DTMF

        [Description("EarlyMedia")]
        EarlyMedia = 6, // EarlyMedia: 還沒200OK 之前的 RTP
    }

    public enum ENUM_PayloadType {
        PT_PCMU = 0,
        PT_GSM = 3,
        PT_G723 = 4,
        PT_LPC = 7,
        PT_PCMA = 8,
        PT_G722 = 9,
        PT_L16_ST = 10,
        PT_L16_MONO = 11,
        PT_G729 = 18
    }

    public enum ENUM_RecRtpSource {
        [Description("None")]
        None = 0, // RTP 在用的

        [Description("Normal")]
        Normal = 1,

        [Description("ForceCloseTalkingNotResp")]
        ForceCloseTalkingNotResponse = 2,

        [Description("ForceCloseHoldNotResp")]
        ForceCloseHoldNotResp = 3,

        [Description("ForceCloseMissingDlg")]
        ForceCloseMissingDlg = 4,

        [Description("ExtnLostDlg")]
        ExtnLostDlg = 5,

        [Description("ExtnMissingDlg")]
        ExtnMissingDlg = 6,

        [Description("TrunkMissingDlg")]
        MissingDlg = 7,

        [Description("Req4XX6XX")]
        Req4XX6XX = 8,

        [Description("SipCancelCall")]
        SipCancelCall = 9
    }

    // IP 的傳送位置
    public enum ENUM_IPDir {
        [Description("未知")]
        Unknown = 0,

        [Description("發送端")]
        Send = 1,   

        [Description("接收端")]
        Recv = 2      
    }


    public enum ENUM_RecType {
        Init = 0,
        EarlyMedia = 1,     // 尚未通話階段...響鈴+語音信箱        
        Talking = 2,        // 已經開始通話        
    }

    public enum ENUM_RecStatus {
        [Description("失敗")]
        Failed = -1,

        [Description("開始")]
        Init = 0,

        [Description("錄音中")]
        Recording = 1,     

        [Description("錄音結束")]
        StopRec = 2,     

        [Description("音檔已產生")]
        RecFileOk = 3,

        [Description("忽略錄音(封包不足)")]
        Ignored = 4,       
    }

    public enum ENUM_ChActiveType {
        [Description("未安裝")]
        Uninstall = 0,

        [Description("停用")]
        Deactivate = 1,

        [Description("啟用")]
        Activate = 2
    };

    public enum ENUM_StationMapping {
        [Description("IpAddr")]
        IpAddr = 1,
        [Description("Mac")]
        Mac = 2
    }

    public enum ENUM_IprBoardType {
        IPRecorder = 253, // IPR Board Model ID
        IPAnalyzer = 254
    }

    public enum ENUM_IprChType {
        IPR_REC = 25,
        IPR_ANA = 26,
    }

    public enum ENUM_RecordingState {
        [Description("閒置")]
        Idle = 0,

        [Description("◉錄音")]
        Actived = 1,

        [Description("▣暫停")]
        Pause = 2
    }

    public enum ENUM_UrsStatus {
        Failed = -1,    // 失敗
        None= 0,        // 一開始
        Init = 1,       // 正在初始化
        NotSupport = 2, // 不支援
        Ok = 3,        
    }


    // 封包的結構類型
    // SipCommand: 用來描述 SIP Signal 的可見文字(可以用 TCP 或 UDP 來傳送)
    // RTP: 由 UDP 傳送，比較少看到 TCP(速度慢)， Header(12 Byte) + 音檔 RawData

    public enum ENUM_UrsCallEvent {
        CallCleared = 1,
        CallConference = 2,
        CallDelivered = 3,
        CallDiverted = 4,
        CallEstablished = 5,
        CallFailed = 6,
        CallHeld = 7,
        CallOriginated = 8,
        CallQueued = 9,
        CallReceived = 10,
        CallRetrieved = 11,
        CallTransferred = 12,
    }

    public enum ENUM_RecordingType {
        [Description("閒置")]
        Idle = 2,

        [Description("SOD")]
        SOD = 3,

        [Description("排程")]
        Schedule = 4,

        [Description("連續")]
        Continuous = 5
    }

    public enum ENUM_RecTriggerType {
        [Description("壓降")]
        Voltage = 0,

        [Description("聲音")]
        Voice = 1,

        [Description("壓降+聲音")]
        VoltageVoice = 2,

        [Description("D-迴路")]
        DChannel = 3,

        [Description("API")]
        API = 4,

        [Description("CTI")]
        CTI = 5,

        [Description("SIP")]
        SIP = 6
    };

    public enum ENUM_CallState {
        [Description("閒置")]
        Idle = 0,

        [Description("響鈴")]
        Ring = 1,

        [Description("通話")]
        Active = 2
    }

    public enum ENUM_ShCodec {
        CODEC_711 = 7,
        CODEC_726 = 17,
        CODEC_MP3 = 85,
        CODEC_GSM = 49
    }

    public enum ENUM_LineStatus {
        [Description("閒置")]
        Idle = 2,

        [Description("響鈴")]
        Ring = 3,

        [Description("外撥")]
        Inbound = 4,

        [Description("來電")]
        Outbound = 5,

        [Description("內線")]
        Intercom = 6,

        [Description("錯誤")]
        Failed = 54
    };


    public enum ENUM_CallSource {
        Inbound = 1,
        Outbound = 2,
        Unknown = 3
    }

    public enum ENUM_CallDir {
        Inbound = 1,
        Outbound = 2
    };

    public enum ENUM_UrsCallType {
        Inbound = 4,
        Outbound = 5,
        Unknown = 0
    }

    public enum ENUM_SqlLogType : int {
        [Description("SqlTrace")]
        Trace = 0,

        [Description("SqlError")]
        Error = 1
    }

    public enum ENUM_LogType {
        [Description("訊息")]
        Info = 1,
        [Description("告警")]
        Alarm = 2,
        [Description("錯誤")]
        Error = 3,
        [Description("嚴重")]
        Fatal = 4
    }    

    public enum ENUM_ApiResultCode : int {
        [Description("Ok")]
        OK = 0,

        [Description("讀取API的Stream錯誤")]
        ReadRequestStreamError = -1,

        [Description("Json字串轉換Model錯誤")]
        JsonConvertModelError = -2,

        [Description("資料無法寫入資料庫")]
        WriteDBFailed = -3,

        [Description("資料不存在於資料庫")]
        DataNotFoundInDB = -4,

        [Description("資料內部 SQL 錯誤")]
        DBSqlError = -5,

        [Description("Request的資料不符合")]
        IncorrectRequestData = -6,

        [Description("內部程序錯誤")]
        ServerInternalError = -7,

        [Description("上傳資料為空")]
        RequestStreamIsEmptyOrNull = -8,

        [Description("token認證錯誤")]
        InvalidToken = -9,

        [Description("token已過期")]
        TokenTimeout = -10,

        [Description("token權限不足")]
        TokenForbiden = -11,

        [Description("錯誤的帳號或密碼")]
        InvalidAccountOrPassword = -12,

        [Description("檔案不存在")]
        FileNotFound = -13,

        [Description("檔案已經存在")]
        FileAlreadyExist = -14,

        [Description("讀取音檔失敗")]
        ReadFileFailed = -15,
    }

}