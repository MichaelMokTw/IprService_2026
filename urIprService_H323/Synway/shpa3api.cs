using System;
using System.Runtime.InteropServices;
using System.Text;


/// <summary>
/// 狡籹蹲 5440 臱笆 Demo: 
/// D:\ShCti5.4.4.0_demo_en\Recorder_IPR\Sourcecode\C#\IPRecorder\MonitorByIP
/// ヘ魁 shpa3api.cs
/// 猔種: MediaParam Τ拜肈既р硂蛤闽code mark
/// </summary>
namespace shpa3api {
    /// <summary>
    /// SsmApi的摘要说明。
    /// </summary>
    //-----------------------------------------------------------------------
    // definition of the mode of the MediaParam
    //-----------------------------------------------------------------------
    public enum MediaParamMode {
        IPM_SENDRECV = 0,
        IPM_RECVONLY = 1,
        IPM_SENDONLY = 2,
    }

    //-----------------------------------------------------------------------
    // definition of channel type, which can be retrieved by invoking
    // function "SsmGetFlag()" and set by invoking "SsmSetFlag()"
    //-----------------------------------------------------------------------
    public enum ChFlag {
        F_RCVDTMFSENS = 1,					    //接收DTMF敏感度
        F_TXDTMFAMP = 2,						//发送DTMF信号强度
        F_RCVPHONUMHOLDUP = 3,				    //被叫号码拦截标记
        F_RELATIVEENGYHOOKDETECT = 4,			//是否启用模拟电话线被叫摘机检测新算法
        F_RXR2FILTERTIME = 5,					//R2接收滤波时间
        F_RECTOFILEA_CALLBACKTIME = 6,
        F_CALLERIDSTYLE = 7,
        F_InVoiceToBus = 8,
        F_ClearInVoiceOnRcvDtmf = 9,
        F_MixerResToBus = 10,
        F_HighAndLowFreqEnScale = 11,
        F_DualAndAllFreqEnScale = 12,
        F_EchoCancelInFsk = 13,				    //设置Fsk收发过程中的回波开关
        F_ChToRingingOnRingCnt = 14,
        F_ISDNNet_WaitRemotePickup = 15,
        F_ClearInVoiceOnRcv450Hz = 16
    };

    //-----------------------------------------------------------------------
    // definition of channel type, which can be retrieved by invoking
    // function "SsmGetChType()"
    //-----------------------------------------------------------------------
    /*
    enum{
        ANALOG_CH=0,
        INTER_CH=1,
        USER_CH=2,
        REC_CH=3,
        SS1_CH=4,
        FAX_CH=5,
        TUP_CH=6,
        ISDN_USER_CH=7,		
        ISDN_NET_CH = 8,
        SOFTFAX_CH = 9,
        MAGNET_CH = 10
    };
    */

    //-----------------------------------------------------------------------
    // definition of blocking reason, which can be retrieved by invoking
    // function "SsmGetBlockReason()"
    //-----------------------------------------------------------------------
    public enum BlockReason {
        BLOCKBY_NULL,
        BLOCKBY_TupRcvMGB,
        BLOCKBY_TupRcvHGB,
        BLOCKBY_TupRcvSGB,
        BLOCKBY_TupWaitRLGTimeout,
        BLOCKBY_TupBlockByApp,
    };

    //-----------------------------------------------------------------------
    // Definition of status on blocking remote circuit or circuit groups,
    // which might be used by following functions
    //		1. "SsmChkBlockRemoteXXX()"
    //		2. "SsmChkBlockRemoteXXX()"
    //-----------------------------------------------------------------------
    public enum RemoteBlockStatus {
        BLOCKREMOTE_Unblocked = 0,	// no block-signal is issued
        BLOCKREMOTE_Blocked = 1,	// is in blocked state now
        BLOCKREMOTE_WaitBlockAck = 2,	// waiting for acknowledgement signal after sending block-signal
        BLOCKREMOTE_WaitUnblockAck = 3,	// waiting for acknowledgement signal after sending unblock-signal
    };

    //-----------------------------------------------------------------------
    // Definition of local blocking status on circuit or circuit groups,
    // which might be used by following functions
    //		 "SsmQueryLocalXXXBlockState()"
    //-----------------------------------------------------------------------
    public enum LocalBlockStatus {
        BLOCK_AppBlockCic = 1,	// channel is blocked by invoking SsmBlockLocalCh()
        BLOCK_TupRcvBLO = 2,	// channel is blocked by received BLO
        BLOCK_TupRcvSGB = 4,	// channel is blocked by received SGB
        BLOCK_TupRcvHGB = 8,	// channel is blocked by received HGB
        BLOCK_TupRcvMGB = 16,	// channel is blocked by received MGB
        BLOCK_AppBlockPCM = 32,	// channel is blocked by invoking SsmBlockLocalPCM()
    }

    //-----------------------------------------------------------------------
    // Definition of channel unavailable reason
    //-----------------------------------------------------------------------
    public enum ReasonUnavailable {
        UNAVAILABLE_PcmSyncLos = 1,
        UNAVAILABLE_Mtp3Unusuable = 2,
    }
    //-----------------------------------------------------------------------
    // 函数调用失败原因的常量定义，用于函数SsmGetLastErrCode()返回值
    //-----------------------------------------------------------------------
    public enum ErrorReason {
        C_ERROR_INIT_FAILURE = 0,
        C_ERROR_SSMAPI_UNOPENED = 1,
        C_ERROR_INVALID_APPCH = 2,
        C_ERROR_UNSUPPORTED_OP = 3,
        C_ERROR_INDEX_UNOPENED = 4,
        C_ERROR_INVALID_BUSCH = 5,
        C_ERROR_OP_UNOPENED = 6,
        C_ERROR_INVALID_FORMAT = 7,
        C_ERROR_INVALID_PARAMETER = 8,
        C_ERROR_FILEOP_FAILURE = 9,
        C_ERROR_MEMORY_FAILURE = 10,
        C_ERROR_RESOURCE_USEUP = 11,
        C_ERROR_SYSTEM = 12,
        C_ERROR_IdleChNotFound = 13,
        C_ERROR_OP_FAILURE = 14,
        C_ERROR_INVALID_APPSPYCIC = 15,
        C_ERROR_FAX_NOFILE = 16,
        C_ERROR_VCH_INVALID_SCALE = 17,
        C_ERROR_DTMF_NOT_SUPPORT = 18, /*不支持这种DTMF方式*/
        C_ERROR_SLAVER_RES_NOT_MATCH = 19,
        C_ERROR_ALL_RES_USED = 20
    };

    //-----------------------------------------------------------------------
    // 自动拨号任务失败的常量定义，用于函数SsmGetAutoDialFailureReason()返回值
    //-----------------------------------------------------------------------
    public enum AutoDialFailureReason {
        ATDL_NULL = 0,								    // 没有呼出任务
        ATDL_Cancel = 1,								// 应用程序主动取消
        ATDL_WaitDialAnsTimeout = 2,					// 等待被叫应答超时
        ATDL_WaitRemotePickupTimeout = 3,				// 等待被叫摘机超时
        ATDL_PcmSyncLos = 4,							// PCM链路基本祯失步超过100ms

        ATDL_Mtp3Unusable = 10,						    // No.7信令：信令不可用
        ATDL_RcvSSB = 11,								// No.7信令：收到SSB
        ATDL_RcvSLB = 12,								// No.7信令：收到SLB
        ATDL_RcvSTB = 13,								// No.7信令：收到STB
        ATDL_RcvUNN = 14,								// No.7信令：收到UNN
        ATDL_RcvSEC = 15,								// No.7信令：收到SEC
        ATDL_RcvCGC = 16,								// No.7信令：收到CGC
        ATDL_RcvNNC = 17,								// No.7信令：收到NNC
        ATDL_RcvCFL = 18,								// No.7信令：收到CFL
        ATDL_RcvLOS = 19,								// No.7信令：收到LOS
        ATDL_RcvSST = 20,								// No.7信令：收到SST
        ATDL_RcvACB = 21,								// No.7信令：收到ACB
        ATDL_RcvDPN = 22,								// No.7信令：收到DPN
        ATDL_RcvEUM = 23,								// No.7信令：收到EUM
        ATDL_RcvADI = 24,								// No.7信令：收到ADI
        ATDL_RcvBLO = 25,								// No.7信令：收到BLO
        ATDL_DoubleOccupy = 26,						// No.7信令：检出同抢
        ATDL_CircuitReset = 27,						// No.7信令：收到电路/群复原信号
        ATDL_BlockedByRemote = 28,					// No.7信令：电路被对端交换机闭塞

        ATDL_SS1WaitOccupyAckTimeout = 40,			// No.1信令：等待占用应答信号超时
        ATDL_SS1RcvCAS_HANGUP = 41,					// No.1信令：收到后向拆线信号
        ATDL_SS1RcvA4 = 42,							// No.1信令：收到A4信号（机键拥塞）
        ATDL_SS1RcvA5 = 43,							// No.1信令：收到A5信号（空号）
        ATDL_SS1RcvUndefinedAx = 44,					// No.1信令：收到未定义的后向A组信号
        ATDL_SS1RcvUndefinedAxOnTxCallerId = 45,		// No.1信令：送主叫时收到未定义的后向A组信号
        ATDL_SS1WaitAxTimeout = 46,					// No.1信令：等候接收后向A组信号超时
        ATDL_SS1WaitAxStopTimeout = 47,				// No.1信令：等候后向A组信号停发超时
        ATDL_SS1WaitAxTimeoutOnTxCallerId = 48,		// No.1信令：送主叫时等候后向A组信号超时
        ATDL_SS1WaitAxStopTimeoutOnTxCallerId = 49,	// No.1信令：送主叫时等候后向A组信号停发超时
        ATDL_SS1RcvKB2 = 50,							// No.1信令：收到KB2信号(用户“市忙”)
        ATDL_SS1RcvKB3 = 51,							// No.1信令：收到KB3信号(用户“长忙”)
        ATDL_SS1RcvKB4 = 52,							// No.1信令：收到KB4信号（机键拥塞）
        ATDL_SS1RcvKB5 = 53,							// No.1信令：收到KB5信号（空号）
        ATDL_SS1RcvUndefinedKB = 54,					// No.1信令：收到未定义的KB信号
        ATDL_SS1WaitKBTimeout = 55,					// No.1信令：接收后向KB信号超时
        ATDL_SS1WaitKBStopTimeout = 56,				// No.1信令：等候被叫方停发后向KB信号超时

        ATDL_ISDNNETISBUS = 60,					//ISDN：网络忙
        ATDL_ISDNEMPTYNO = 61,					//ISDN:所拨的是空号.

        ATDL_IllegalMessage = 65,                   //SS7:非法消息
        ATDL_RcvREL = 66,                   //ISUP:收到释放消息
        ATDL_RcvCBK = 67,                   //TUP: Rcv CBK Dial Failure
    };



    //-----------------------------------------------------------------------
    // 自动拨号任务执行情况的常量定义，函数SsmChkAutoDial()的返回值
    //-----------------------------------------------------------------------
    public enum CheckAutoDial {
        DIAL_STANDBY = 0,	// 没有自动拨号任务
        DIAL_DIALING = 1,	// 正在自动拨号
        DIAL_ECHOTONE = 2,	// 发送完被叫号码后检测到了回铃音
        DIAL_NO_DIALTONE = 3,	// 没有拨号音，自动拨号失败。
        DIAL_BUSYTONE = 4,	// 被叫用户忙，自动拨号结束
        DIAL_ECHO_NOVOICE = 5,	// 模拟外线通道拨号结束并收到回铃音后出现无声，自动拨号结束
        DIAL_NOVOICE = 6,	// 模拟外线通道拨号结束后在指定时间内没有检测到任何声音，自动拨号结束
        DIAL_VOICE = 7,	// 被叫用户摘机，自动拨号结束
        DIAL_VOICEF1 = 8,	// 被叫用户摘机且收到频率F1的声音（模拟外线通道），自动拨号结束
        DIAL_VOICEF2 = 9,	// 被叫用户摘机且收到频率F2的声音（模拟外线通道），自动拨号结束
        DIAL_NOANSWER = 10,	// 无人接听，自动拨号失败
        DIAL_FAILURE = 11,	// 自动拨号失败
        DIAL_INVALID_PHONUM = 12,	// 空号，自动拨号结束
    };

    //-----------------------------------------------------------------------
    // 通道挂起原因常量定义，函数SsmGetPendingReason()的返回值
    //-----------------------------------------------------------------------
    public enum PendReason {
        ANALOGOUT_NO_DIALTONE = 0,				//模拟通道：自动拨号时没有检测到拨号音，自动拨号失败。
        ANALOGOUT_BUSYTONE = 1,					//模拟通道：自动拨号结束后检测到拨号音，自动拨号失败。
        ANALOGOUT_ECHO_NOVOICE = 2,				//模拟通道：自动拨号结束后并检测到回铃后出现无。
        ANALOGOUT_NOANSWER = 3,					//模拟通道：自动拨号结束后检测到回铃信号但在指定的时间内没有应答。
        ANALOGOUT_TALKING_REMOTE_HANGUPED = 4,	//模拟通道：在"通话"状态时检测对方挂机
        ANALOGOUT_NOVOICE = 5,					//模拟通道：自动拨号结束后检测到线路上出现无声

        PEND_WaitBckStpMsg = 10,					//数字中继通道：等待应用程序设置被叫用户状态

        SS1IN_BWD_KB5 = 11,						//No.1信令通道：等待主叫拆线
        PEND_RemoteHangupOnTalking = 12,			//数字中继通道：自动呼入进入通话后检测到主叫用户先挂机

        PEND_AutoDialFailed = 13,					//数字中继通道：自动拨号失败
        PEND_SsxUnusable = 14,					//数字中继通道：信令不可用
        PEND_CircuitReset = 15,					//数字中继通道：电路复原
        PEND_PcmSyncLos = 16,						//数字中继通道：基本祯同步丢失时间超过100ms

        SS1OUT_TALKING_REMOTE_HANGUPED = 20,		//数字中继通道：自动呼出进入通话后检测到被叫用户先挂机
        PEND_CalleeHangupOnTalking = 20,			//数字中继通道：自动呼出进入通话后检测到被叫用户先挂机

        SS1OUT_NOANSWER = 21,						//No.1信令通道：等待被叫用户摘机超时
        SS1OUT_NOBWDACK = 22,						//No.1信令通道：等待占用应答信号超时
        SS1OUT_DIALING_BWD_HANGUP = 23,			//No.1信令通道：收到后向拆线信号
        SS1OUT_BWD_A5 = 24,						//No.1信令通道：收到A=5（空号）信号
        SS1OUT_BWD_KB5 = 25,						//No.1信令通道：收到KB=5（空号）信号
        SS1OUT_BWD_KB2 = 26,						//No.1信令通道：用户“市忙”
        SS1OUT_BWD_KB3 = 27,						//No.1信令通道：用户”长忙“
        SS1OUT_BWD_A4 = 28,						//No.1信令通道：机键拥塞
        SS1OUT_BWD_KB4 = 29,						//No.1信令通道：收到KB=4（机键拥塞）信号
        SS1OUT_TIMEOUT_BWD_A = 30,				//No.1信令通道：等候接收后向A组信号超时
        SS1OUT_TIMEOUT_BWD_A_STOP = 31,			//No.1信令通道：等候后向A组信号停发超时
        SS1OUT_TIMEOUT_BWD_KB = 32,				//No.1信令通道：接收后向KB信号超时
        SS1OUT_TIMEOUT_BWD_KB_STOP = 33,			//No.1信令通道：等候被叫方停发后向KB信号超时
        SS1OUT_TIMEOUT_CALLERID_BWD_A1 = 34,		//No.1信令通道：收到未定义的后向A组信号
        SS1OUT_TIMEOUT_CALLERID_BWD_A1_STOP = 35,	//No.1信令通道：发送CALLERID时等候后向A组信号停发超时
        SS1OUT_UNDEFINED_CALLERID_BWD_A = 36,		//No.1信令通道：发送主叫号码时收到未定义的后向A组信号
        SS1OUT_UNDEFINED_BWD_A = 37,				//No.1信令通道：收到未定义的后向A组信号
        SS1OUT_UNDEFINED_BWD_KB = 38,				//No.1信令通道：收到未定义的KB信号

        ISDN_CALLOVER = 41,				//呼叫结束,对方先挂机.
        ISDN_WAIT_RELEASE = 42,				//等待释放
        ISDN_HANGING = 43,				//拆线中
        ISDN_RELEASING = 44,				//正在释放

        ISDN_UNALLOCATED_NUMBER = 45,			//ISDN，未分配的号码
        ISDN_NETWORK_BUSY = 46,			//ISDN, 网络忙。
        ISDN_CIRCUIT_NOT_AVAILABLE = 47,			//ISDN, 指定的电路不可用。
        PEND_CalleeHangupOnWaitRemotePickUp = 48,	//数字中继通道：自动呼出后等待被叫挂机时检测到被叫用户先挂机

        ISUP_HardCircuitBlock = 49,
        ISUP_RemoteSuspend = 50,

        PEND_RcvHGBOrSGB = 51,			//接收到对端交换机SGB/HGB后驱动错误处理

        ISDN_NO_ANSWER = 52,			//ISDN, 无应答
        ISDN_CALL_REJ = 53,			//ISDN, 呼叫拒绝
    };

    //-----------------------------------------------------------------------
    // 通道状态常量定义，函数SsmGetChState()的返回值
    //-----------------------------------------------------------------------
    public enum ChState {
        S_CALL_STANDBY = 0,					//“空闲”状态
        S_CALL_PICKUPED = 1,					//“摘机”状态
        S_CALL_RINGING = 2,					//“振铃”状态
        S_CALL_TALKING = 3,					//“通话”状态

        S_CALL_ANALOG_WAITDIALTONE = 4,		// “等待拨号音”状态  （模拟通道）
        S_CALL_ANALOG_TXPHONUM = 5,		// “拨号”状态        （模拟通道）
        S_CALL_ANALOG_WAITDIALRESULT = 6,		// “等待拨号结果”状态（模拟通道）

        S_CALL_PENDING = 7,		// “挂起”状态
        S_CALL_OFFLINE = 8,		// “断线”状态        （录音通道）
        S_CALL_WAIT_REMOTE_PICKUP = 9,		// “等待被叫摘机”状态
        S_CALL_ANALOG_CLEAR = 10,		//  **内部状态**       （模拟通道）
        S_CALL_UNAVAILABLE = 11,		// “通道不可用”状态
        S_CALL_LOCKED = 12,		// “呼出锁定”状态

        S_CALL_RemoteBlock = 19,		// “对端闭塞”状态
        S_CALL_LocalBlock = 20,		// “本端闭塞”状态

        S_CALL_Ss1InWaitPhoNum = 30,		// “等待接收被叫号码”状态			（No.1信令呼入）
        S_CALL_Ss1InWaitFwdStop = 31,		// “等待前向停发”状态				（No.1信令呼入）
        S_CALL_Ss1InWaitCallerID = 32,		// “等待接收CALLERID号码”状态		（No.1信令呼入）
        S_CALL_Ss1InWaitKD = 33,		// “等待接收KD信号”状态			（No.1信令呼入）
        S_CALL_Ss1InWaitKDStop = 34,		// “等待KD信号停发”状态			（No.1信令呼入）
        S_CALL_SS1_SAYIDLE = 35,		// “发送示闲信令”状态				（No.1信令）
        S_CALL_SS1WaitIdleCAS = 36,		// “等待对端示闲”状态				（No.1信令）
        S_CALL_SS1PhoNumHoldup = 37,		// “冗余号码拦截”状态				（No.1信令）
        S_CALL_Ss1InWaitStopSendA3p = 38,		// “等待停发A3p信号”状态			（No.1信令）


        S_CALL_Ss1OutWaitBwdAck = 40,	// “等待后向占用证实信令”状态		（No.1信令呼出）
        S_CALL_Ss1OutTxPhoNum = 41,	// “发送被叫号码”状态				（No.1信令呼出）
        S_CALL_Ss1OutWaitAppendPhoNum = 42,	// “等待应用程序追加电话号码”状态	（No.1信令呼出）
        S_CALL_Ss1OutTxCallerID = 43,	// “发送主叫号码”状态				（No.1信令呼出）
        S_CALL_Ss1OutWaitKB = 44,	// “等待接收KB信号”状态			（No.1信令呼出）
        S_CALL_Ss1OutDetectA3p = 45,	// “检测A3p信号(脉冲)”状态		（No.1信令呼出）


        S_FAX_Wait = S_CALL_STANDBY,		// “空闲”状态								（传真通道）
        S_FAX_ROUND = 50,					// “状态转移过程中”状态					（传真通道）
        S_FAX_PhaseA = 51,					// “传真呼叫建立”状态						（传真通道）
        S_FAX_PhaseB = 52,					// “传真报文前处理”状态					（传真通道）
        S_FAX_SendDCS = 53,					// “传真发送中向接收方发送DCS信号”状态    （传真通道）
        S_FAX_Train = 54,					// “传真报文传输前传输训练”状态			（传真通道）
        S_FAX_PhaseC = 55,					// “传真报文传输中”状态					（传真通道）
        S_FAX_PhaseD = 56,					// “传真报文后处理”状态					（传真通道）
        S_FAX_NextPage = 57,					// “传真报文传输下一页”状态				（传真通道）
        S_FAX_AllSent = 58,					// “传真发送中报文传输结束”状态			（传真通道）
        S_FAX_PhaseE = 59,					// “传真呼叫释放”状态						（传真通道）
        S_FAX_Reset = 60,					// “复位MODEM”状态						（传真通道）
        S_FAX_Init = 61,					// “初始化MODEM”状态						（传真通道）
        S_FAX_RcvDCS = 62,					// “传真接收中接收发方的DCS信号”状态		（传真通道）
        S_FAX_SendFTT = 63,					// “传真接收中向发方发送训练失败信号FTT”状态		（传真通道）
        S_FAX_SendCFR = 64,					// “传真接收中向发方发送可接受的证实信号CFR”状态  （传真通道）

        S_TUP_WaitPcmReset = 70,			// “等待电路群复原”状态		（No.7信令TUP协议）
        S_TUP_WaitSAM = 71,			// “等待后续地址消息”状态		（No.7信令TUP协议）
        S_TUP_WaitGSM = 72,			// “等待GSM消息”状态			（No.7信令TUP协议）
        S_TUP_WaitCLF = 73,			// “等待主叫拆线信号”状态		（No.7信令TUP协议）
        S_TUP_WaitPrefix = 74,			// “等待接收入局字冠”状态		（No.7信令TUP协议）
        S_TUP_WaitDialAnswer = 75,			// “等待拨号结果”状态			（No.7信令TUP协议）
        S_TUP_WaitRLG = 76,			// “等待释放监护信号”状态		（No.7信令TUP协议）
        S_TUP_WaitSetCallerID = 77,         //  "等待设置主叫"状态          （No.7信令TUP协议）

        S_ISDN_OUT_WAIT_NET_RESPONSE = 81,	//ISDN：等待网络响应
        S_ISDN_OUT_PLS_APPEND_NO = 82,	//ISDN：请追加号码
        S_ISDN_IN_CHK_CALL_IN = 83,	//ISDN：检测到呼入
        S_ISDN_IN_RCVING_NO = 84,	//ISDN：正在接收号码
        S_ISDN_IN_WAIT_TALK = 85,	//ISDN：准备进入通话
        S_ISDN_OUT_WAIT_ALERT = 86,	//ISDN: 等待对方发提醒信号

        S_ISDN_CALL_BEGIN = 87,	//ISDN：呼出时为刚发起呼叫，呼入时为刚检测到呼入
        S_ISDN_WAIT_HUANGUP = 88,	//ISDN:等待释放完成

        S_CALL_SENDRING = 100,  //磁石模块正在发送振铃

        S_SPY_STANDBY = S_CALL_STANDBY,	//监控：空闲
        S_SPY_RCVPHONUM = 105,				//监控：接收号码
        S_SPY_RINGING = S_CALL_RINGING,	//监控：振铃
        S_SPY_TALKING = S_CALL_TALKING,	//监控：通话

        S_SPY_SS1RESET = 110,	//SS1监控：复原
        S_SPY_SS1WAITBWDACK = 111,	//SS1监控：等待后向证实
        S_SPY_SS1WAITKB = 112,	//SS1监控：等待KB

        S_ISUP_WaitSAM = 120,// ISUP:等待后继号码
        S_ISUP_WaitRLC = 121,// ISUP:等待释放完成消息
        S_ISUP_WaitReset = 122,// ISUP:电路复原
        S_ISUP_LocallyBlocked = 123,// ISUP:本地闭塞，即本地闭塞远端呼出
        S_ISUP_RemotelyBlocked = 124,// ISUP:远端闭塞，即远端闭塞本端呼出
        S_ISUP_WaitDialAnswer = 125,// ISUP:等待呼出结果
        S_ISUP_WaitINF = 126,// ISUP:等待主叫号码
        S_ISUP_WaitSetCallerID = 127,// ISUP:等待设置主叫
        S_DTRC_ACTIVE = 128,// DTRC:被监控话路处于非空闲状态

        S_ISUP_Suspend = 129,//	ISUP:通话后收到暂停

        S_CALL_EM_TXPHONUM = 130, // “拨号”状态  （EM通道）
        S_CALL_EM_WaitIdleCAS = 131, // “等待对端示闲”状态 （EM通道）
        S_CALL_VOIP_DIALING = 132,     //VoIP主叫拨号状态
        S_CALL_VOIP_WAIT_CONNECTED = 133,  //VoIP被叫摘机等待进入通话状态
        S_CALL_VOIP_CHANNEL_UNUSABLE = 134,  //VoIP通道目前不可用

        S_CALL_DISCONECT = 135,    //USB断线

        S_CALL_SS1WaitFlashEnd = 136,
        S_CALL_FlashEnd = 137,
        S_CALL_SIGNAL_ERROR = 139,
        S_CALL_FRAME_ERROR = 140,

        //通道状态值150-159为IP卡预留
        S_CALL_VOIP_SESSION_PROCEEDING = 150,		//SIP会话处理中
        S_CALL_VOIP_REG_ING = 151,			//SIP通道注册中
        S_CALL_VOIP_REG_FAILED = 152,		//SIP通道注册失败
        //通道状态值160-169为IP资源卡预留
        //S_IP_MEIDA_IDLE 				= S_CALL_STANDBY,
        S_IP_MEDIA_LOCK = 160,
        S_IP_MEDIA_OPEN = 161,
        S_IPR_USING = 170,	//added by wangfeng for synIPR, 2011.06.13
        S_IPR_COMMUNICATING = 171,	//added by netwolf for SynIPR Master Communicate with Slaver,2011.07.28

    };

    public enum DSTEvent {
        DST_OFFHOOK = 0x8,
        DST_ONHOOK = 0xe,
        DST_LT_ON = 0x1001,
        DST_LT_OFF = 0x1002,
        DST_LT_FLASHING = 0x1003,
        DST_DGT_PRS = 0x1006,
        DST_DGT_RLS = 0x1007,
        DST_MSG_CHG = 0x1008,
        DST_STARTSTOP_ON = 0x1009,
        DST_STARTSTOP_OFF = 0x100a,
        DST_LT_FASTFLASHING = 0x100b,
        DST_DOWNLOAD_STATUS = 0x100c,
        DST_FINISHED_PLAY = 0x100d,
        DST_FUNC_BTN_PRS = 0x100e,
        DST_FUNC_BTN_RLS = 0x100f,
        DST_HOLD_BTN_PRS = 0x1010,
        DST_HOLD_BTN_RLS = 0x1011,
        DST_RELEASE_BTN_PRS = 0x1012,
        DST_RELEASE_BTN_RLS = 0x1013,
        DST_TRANSFER_BTN_PRS = 0x1014,
        DST_ANSWER_BTN_PRS = 0x1015,
        DST_SPEAKER_BTN_PRS = 0x1016,
        DST_REDIAL_BTN_PRS = 0x1017,
        DST_CONF_BTN_PRS = 0x1018,
        DST_RECALL_BTN_PRS = 0x1019,
        DST_FEATURE_BTN_PRS = 0x101a,
        DST_UP_DOWN = 0x101b,
        DST_EXIT_BTN_PRS = 0x101c,
        DST_HELP_BTN_PRS = 0x101d,
        DST_SOFT_BTN_PRS = 0x101e,
        DST_RING_ON = 0x101f,
        DST_RING_OFF = 0x1020,
        DST_LINE_BTN_PRS = 0x1021,
        DST_MENU_BTN_PRS = 0x1022,
        DST_PREVIOUS_BTN_PRS = 0x1023,
        DST_NEXT_BTN_PRS = 0x1024,
        DST_LT_QUICKFLASH = 0x1025,
        DST_AUDIO_ON = 0x1026,
        DST_AUDIO_OFF = 0x1027,
        DST_DISPLAY_CLOCK = 0x1028,
        DST_DISPLAY_TIMER = 0x1029,
        DST_DISPLAY_CLEAR = 0x102a,
        DST_CFWD = 0x102b,
        DST_CFWD_CANCELED = 0x102c,
        DST_AUTO_ANSWER = 0x102d,
        DST_AUTO_ANSWER_CANCELED = 0x102e,
        DST_SET_BUSY = 0x102f,
        DST_SET_BUSY_CANCELED = 0x1030,
        DST_DESTINATION_BUSY = 0x1031,
        DST_REORDER = 0x1032,
        DST_LT_VERY_FASTFLASHING = 0x1033,
        DST_SPEAKER_BTN_RLS = 0x1034,
        DST_REDIAL_BTN_RLS = 0x1035,
        DST_TRANSFER_BTN_RLS = 0x1036,
        DST_CONF_BTN_RLS = 0x1037,
        DST_DISCONNECTED = 0x1038,
        DST_CONNECTED = 0x1039,
        DST_ABANDONED = 0x103a,
        DST_SUSPENDED = 0x103b,
        DST_RESUMED = 0x103c,
        DST_HELD = 0x103d,
        DST_RETRIEVED = 0x103e,
        DST_REJECTED = 0x103f,
        DST_MSG_BTN_PRS = 0x1040,
        DST_MSG_BTN_RLS = 0x1041,
        DST_SUPERVISOR_BTN_PRS = 0x1042,
        DST_SUPERVISOR_BTN_RLS = 0x1043,
        DST_WRAPUP_BTN_PRS = 0x1044,
        DST_WRAPUP_BTN_RLS = 0x1045,
        DST_READY_BTN_PRS = 0x1046,
        DST_READY_BTN_RLS = 0x1047,
        DST_LOGON_BTN_PRS = 0x1048,
        DST_BREAK_BTN_PRS = 0x1049,
        DST_AUDIO_CHG = 0x104a,
        DST_DISPLAY_MSG = 0x104b,
        DST_WORK_BTN_PRS = 0x104c,
        DST_TALLY_BTN_PRS = 0x104d,
        DST_PROGRAM_BTN_PRS = 0x104e,
        DST_MUTE_BTN_PRS = 0x104f,
        DST_ALERTING_AUTO_ANSWER = 0x1050,
        DST_MENU_BTN_RLS = 0x1051,
        DST_EXIT_BTN_RLS = 0x1052,
        DST_NEXT_BTN_RLS = 0x1053,
        DST_PREVIOUS_BTN_RLS = 0x1054,
        DST_SHIFT_BTN_PRS = 0x1055,
        DST_SHIFT_BTN_RLS = 0x1056,
        DST_PAGE_BTN_PRS = 0x1057,
        DST_PAGE_BTN_RLS = 0x1058,
        DST_SOFT_BTN_RLS = 0x1059,
        DST_LINE_LT_OFF = 0x1060,
        DST_LINE_LT_ON = 0x1061,
        DST_LINE_LT_FLASHING = 0x1062,
        DST_LINE_LT_FASTFLASHING = 0x1063,
        DST_LINE_LT_VERY_FASTFLASHING = 0x1064,
        DST_LINE_LT_QUICKFLASH = 0x1065,
        DST_LINE_LT_WINK = 0x1066,
        DST_LINE_LT_SLOW_WINK = 0x1067,
        DST_FEATURE_LT_OFF = 0x1068,
        DST_FEATURE_LT_ON = 0x1069,
        DST_FEATURE_LT_FLASHING = 0x106A,
        DST_FEATURE_LT_FASTFLASHING = 0x106B,
        DST_FEATURE_LT_VERY_FASTFLASHING = 0x106C,
        DST_FEATURE_LT_QUICKFLASH = 0x106D,
        DST_FEATURE_LT_WINK = 0x106E,
        DST_FEATURE_LT_SLOW_WINK = 0x106F,
        DST_SPEAKER_LT_OFF = 0x1070,
        DST_SPEAKER_LT_ON = 0x1071,
        DST_SPEAKER_LT_FLASHING = 0x1072,
        DST_SPEAKER_LT_FASTFLASHING = 0x1073,
        DST_SPEAKER_LT_VERY_FASTFLASHING = 0x1074,
        DST_SPEAKER_LT_QUICKFLASH = 0x1075,
        DST_SPEAKER_LT_WINK = 0x1076,
        DST_SPEAKER_LT_SLOW_WINK = 0x1077,
        DST_MIC_LT_OFF = 0x1078,
        DST_MIC_LT_ON = 0x1079,
        DST_MIC_LT_FLASHING = 0x107A,
        DST_MIC_LT_FASTFLASHING = 0x107B,
        DST_MIC_LT_VERY_FASTFLASHING = 0x107C,
        DST_MIC_LT_QUICKFLASH = 0x107D,
        DST_MIC_LT_WINK = 0x107E,
        DST_MIC_LT_SLOW_WINK = 0x107F,
        DST_HOLD_LT_OFF = 0x1080,
        DST_HOLD_LT_ON = 0x1081,
        DST_HOLD_LT_FLASHING = 0x1082,
        DST_HOLD_LT_FASTFLASHING = 0x1083,
        DST_HOLD_LT_VERY_FASTFLASHING = 0x1084,
        DST_HOLD_LT_QUICKFLASH = 0x1085,
        DST_HOLD_LT_WINK = 0x1086,
        DST_HOLD_LT_SLOW_WINK = 0x1087,
        DST_RELEASE_LT_OFF = 0x1088,
        DST_RELEASE_LT_ON = 0x1089,
        DST_RELEASE_LT_FLASHING = 0x108A,
        DST_RELEASE_LT_FASTFLASHING = 0x108B,
        DST_RELEASE_LT_VERY_FASTFLASHING = 0x108C,
        DST_RELEASE_LT_QUICKFLASH = 0x108D,
        DST_RELEASE_LT_WINK = 0x108E,
        DST_RELEASE_LT_SLOW_WINK = 0x108F,
        DST_HELP_LT_OFF = 0x1090,
        DST_HELP_LT_ON = 0x1091,
        DST_HELP_LT_FLASHING = 0x1092,
        DST_HELP_LT_FASTFLASHING = 0x1093,
        DST_HELP_LT_VERY_FASTFLASHING = 0x1094,
        DST_HELP_LT_QUICKFLASH = 0x1095,
        DST_HELP_LT_WINK = 0x1096,
        DST_HELP_LT_SLOW_WINK = 0x1097,
        DST_SUPERVISOR_LT_OFF = 0x1098,
        DST_SUPERVISOR_LT_ON = 0x1099,
        DST_SUPERVISOR_LT_FLASHING = 0x109A,
        DST_SUPERVISOR_LT_FASTFLASHING = 0x109B,
        DST_SUPERVISOR_LT_VERY_FASTFLASHING = 0x109C,
        DST_SUPERVISOR_LT_QUICKFLASH = 0x109D,
        DST_SUPERVISOR_LT_WINK = 0x109E,
        DST_SUPERVISOR_LT_SLOW_WINK = 0x109F,
        DST_READY_LT_OFF = 0x10A0,
        DST_READY_LT_ON = 0x10A1,
        DST_READY_LT_FLASHING = 0x10A2,
        DST_READY_LT_FASTFLASHING = 0x10A3,
        DST_READY_LT_VERY_FASTFLASHING = 0x10A4,
        DST_READY_LT_QUICKFLASH = 0x10A5,
        DST_READY_LT_WINK = 0x10A6,
        DST_READY_LT_SLOW_WINK = 0x10A7,
        DST_LOGON_LT_OFF = 0x10A8,
        DST_LOGON_LT_ON = 0x10A9,
        DST_LOGON_LT_FLASHING = 0x10AA,
        DST_LOGON_LT_FASTFLASHING = 0x10AB,
        DST_LOGON_LT_VERY_FASTFLASHING = 0x10AC,
        DST_LOGON_LT_QUICKFLASH = 0x10AD,
        DST_LOGON_LT_WINK = 0x10AE,
        DST_LOGON_LT_SLOW_WINK = 0x10AF,
        DST_WRAPUP_LT_OFF = 0x10B0,
        DST_WRAPUP_LT_ON = 0x10B1,
        DST_WRAPUP_LT_FLASHING = 0x10B2,
        DST_WRAPUP_LT_FASTFLASHING = 0x10B3,
        DST_WRAPUP_LT_VERY_FASTFLASHING = 0x10B4,
        DST_WRAPUP_LT_QUICKFLASH = 0x10B5,
        DST_WRAPUP_LT_WINK = 0x10B6,
        DST_WRAPUP_LT_SLOW_WINK = 0x10B7,
        DST_RING_LT_OFF = 0x10B8,
        DST_RING_LT_ON = 0x10B9,
        DST_RING_LT_FLASHING = 0x10BA,
        DST_RING_LT_FASTFLASHING = 0x10BB,
        DST_RING_LT_VERY_FASTFLASHING = 0x10BC,
        DST_RING_LT_QUICKFLASH = 0x10BD,
        DST_RING_LT_WINK = 0x10BE,
        DST_RING_LT_SLOW_WINK = 0x10BF,
        DST_ANSWER_LT_OFF = 0x10C0,
        DST_ANSWER_LT_ON = 0x10C1,
        DST_ANSWER_LT_FLASHING = 0x10C2,
        DST_ANSWER_LT_FASTFLASHING = 0x10C3,
        DST_ANSWER_LT_VERY_FASTFLASHING = 0x10C4,
        DST_ANSWER_LT_QUICKFLASH = 0x10C5,
        DST_ANSWER_LT_WINK = 0x10C6,
        DST_ANSWER_LT_SLOW_WINK = 0x10C7,
        DST_PROGRAM_LT_OFF = 0x10C8,
        DST_PROGRAM_LT_ON = 0x10C9,
        DST_PROGRAM_LT_FLASHING = 0x10CA,
        DST_PROGRAM_LT_FASTFLASHING = 0x10CB,
        DST_PROGRAM_LT_VERY_FASTFLASHING = 0x10CC,
        DST_PROGRAM_LT_QUICKFLASH = 0x10CD,
        DST_PROGRAM_LT_WINK = 0x10CE,
        DST_PROGRAM_LT_MEDIUM_WINK = 0x10CF,
        DST_MSG_LT_OFF = 0x10D0,
        DST_MSG_LT_ON = 0x10D1,
        DST_MSG_LT_FLASHING = 0x10D2,
        DST_MSG_LT_FASTFLASHING = 0x10D3,
        DST_MSG_LT_VERY_FASTFLASHING = 0x10D4,
        DST_MSG_LT_QUICKFLASH = 0x10D5,
        DST_MSG_LT_WINK = 0x10D6,
        DST_MSG_LT_SLOW_WINK = 0x10D7,
        DST_TRANSFER_LT_OFF = 0x10D8,
        DST_TRANSFER_LT_ON = 0x10D9,
        DST_TRANSFER_LT_FLASHING = 0x10DA,
        DST_TRANSFER_LT_FASTFLASHING = 0x10DB,
        DST_TRANSFER_LT_VERY_FASTFLASHING = 0x10DC,
        DST_TRANSFER_LT_QUICKFLASH = 0x10DD,
        DST_TRANSFER_LT_WINK = 0x10DE,
        DST_TRANSFER_LT_MEDIUM_WINK = 0x10DF,
        DST_CONFERENCE_LT_OFF = 0x10E0,
        DST_CONFERENCE_LT_ON = 0x10E1,
        DST_CONFERENCE_LT_FLASHING = 0x10E2,
        DST_CONFERENCE_LT_FASTFLASHING = 0x10E3,
        DST_CONFERENCE_LT_VERY_FASTFLASHING = 0x10E4,
        DST_CONFERENCE_LT_QUICKFLASH = 0x10E5,
        DST_CONFERENCE_LT_WINK = 0x10E6,
        DST_CONFERENCE_LT_MEDIUM_WINK = 0x10E7,
        DST_SOFT_LT_OFF = 0x10E8,
        DST_SOFT_LT_ON = 0x10E9,
        DST_SOFT_LT_FLASHING = 0x10EA,
        DST_SOFT_LT_FASTFLASHING = 0x10EB,
        DST_SOFT_LT_VERY_FASTFLASHING = 0x10EC,
        DST_SOFT_LT_QUICKFLASH = 0x10ED,
        DST_SOFT_LT_WINK = 0x10EE,
        DST_SOFT_LT_SLOW_WINK = 0x10EF,
        DST_MENU_LT_OFF = 0x10F0,
        DST_MENU_LT_ON = 0x10F1,
        DST_MENU_LT_FLASHING = 0x10F2,
        DST_MENU_LT_FASTFLASHING = 0x10F3,
        DST_MENU_LT_VERY_FASTFLASHING = 0x10F4,
        DST_MENU_LT_QUICKFLASH = 0x10F5,
        DST_MENU_LT_WINK = 0x10F6,
        DST_MENU_LT_SLOW_WINK = 0x10F7,
        DST_CALLWAITING_LT_OFF = 0x10F8,
        DST_CALLWAITING_LT_ON = 0x10F9,
        DST_CALLWAITING_LT_FLASHING = 0x10FA,
        DST_CALLWAITING_LT_FASTFLASHING = 0x10FB,
        DST_CALLWAITING_LT_VERY_FASTFLASHING = 0x10FC,
        DST_CALLWAITING_LT_QUICKFLASH = 0x10FD,
        DST_CALLWAITING_LT_WINK = 0x10FE,
        DST_CALLWAITING_LT_SLOW_WINK = 0x10FF,
        DST_REDIAL_LT_OFF = 0x1100,
        DST_REDIAL_LT_ON = 0x1101,
        DST_REDIAL_LT_FLASHING = 0x1102,
        DST_REDIAL_LT_FASTFLASHING = 0x1103,
        DST_REDIAL_LT_VERY_FASTFLASHING = 0x1104,
        DST_REDIAL_LT_QUICKFLASH = 0x1105,
        DST_REDIAL_LT_WINK = 0x1106,
        DST_REDIAL_LT_SLOW_WINK = 0x1107,
        DST_PAGE_LT_OFF = 0x1108,
        DST_PAGE_LT_ON = 0x1109,
        DST_PAGE_LT_FLASHING = 0x110A,
        DST_PAGE_LT_FASTFLASHING = 0x110B,
        DST_PAGE_LT_VERY_FASTFLASHING = 0x110C,
        DST_PAGE_LT_QUICKFLASH = 0x110D,
        DST_CTRL_BTN_PRS = 0x110E,
        DST_CTRL_BTN_RLS = 0x110F,
        DST_CANCEL_BTN_PRS = 0x1110,
        DST_CANCEL_BTN_RLS = 0x1111,
        DST_MIC_BTN_PRS = 0x1112,
        DST_MIC_BTN_RLS = 0x1113,
        DST_FLASH_BTN_PRS = 0x1114,
        DST_FLASH_BTN_RLS = 0x1115,
        DST_DIRECTORY_BTN_PRS = 0x1116,
        DST_DIRECTORY_BTN_RLS = 0x1117,
        DST_HANDSFREE_BTN_PRS = 0x1118,
        DST_HANDSFREE_BTN_RLS = 0x1119,
        DST_RINGTONE_BTN_PRS = 0x111A,
        DST_RINGTONE_BTN_RLS = 0x111B,
        DST_SAVE_BTN_PRS = 0x111C,
        DST_SAVE_BTN_RLS = 0x111D,
        DST_MUTE_LT_OFF = 0x111E,
        DST_MUTE_LT_ON = 0x111F,
        DST_MUTE_LT_FLASHING = 0x1120,
        DST_MUTE_LT_FASTFLASHING = 0x1121,
        DST_MUTE_LT_VERY_FASTFLASHING = 0x1122,
        DST_MUTE_LT_QUICKFLASH = 0x1123,
        DST_MUTE_LT_WINK = 0x1124,
        DST_MUTE_LT_SLOW_WINK = 0x1125,
        DST_MUTE_LT_MEDIUM_WINK = 0x1126,
        DST_HANDSFREE_LT_OFF = 0x1127,
        DST_HANDSFREE_LT_ON = 0x1128,
        DST_HANDSFREE_LT_FLASHING = 0x1129,
        DST_HANDSFREE_LT_FASTFLASHING = 0x112A,
        DST_HANDSFREE_LT_VERY_FASTFLASHING = 0x112B,
        DST_HANDSFREE_LT_QUICKFLASH = 0x112C,
        DST_HANDSFREE_LT_WINK = 0x112D,
        DST_HANDSFREE_LT_SLOW_WINK = 0x112E,
        DST_HANDSFREE_LT_MEDIUM_WINK = 0x112F,
        DST_DIRECTORY_LT_OFF = 0x1130,
        DST_DIRECTORY_LT_ON = 0x1131,
        DST_DIRECTORY_LT_FLASHING = 0x1132,
        DST_DIRECTORY_LT_FASTFLASHING = 0x1133,
        DST_DIRECTORY_LT_VERY_FASTFLASHING = 0x1134,
        DST_DIRECTORY_LT_QUICKFLASH = 0x1135,
        DST_DIRECTORY_LT_WINK = 0x1136,
        DST_DIRECTORY_LT_SLOW_WINK = 0x1137,
        DST_DIRECTORY_LT_MEDIUM_WINK = 0x1138,
        DST_RINGTONE_LT_OFF = 0x1139,
        DST_RINGTONE_LT_ON = 0x113A,
        DST_RINGTONE_LT_FLASHING = 0x113B,
        DST_RINGTONE_LT_FASTFLASHING = 0x113C,
        DST_RINGTONE_LT_VERY_FASTFLASHING = 0x113D,
        DST_RINGTONE_LT_QUICKFLASH = 0x113E,
        DST_RINGTONE_LT_WINK = 0x113F,
        DST_RINGTONE_LT_SLOW_WINK = 0x1140,
        DST_RINGTONE_LT_MEDIUM_WINK = 0x1141,
        DST_SAVE_LT_OFF = 0x1142,
        DST_SAVE_LT_ON = 0x1143,
        DST_SAVE_LT_FLASHING = 0x1144,
        DST_SAVE_LT_FASTFLASHING = 0x1145,
        DST_SAVE_LT_VERY_FASTFLASHING = 0x1146,
        DST_SAVE_LT_QUICKFLASH = 0x1147,
        DST_SAVE_LT_WINK = 0x1148,
        DST_SAVE_LT_SLOW_WINK = 0x1149,
        DST_SAVE_LT_MEDIUM_WINK = 0x114A,
        DST_FUNC_LT_WINK = 0x114B,
        DST_FUNC_LT_SLOW_WINK = 0x114C,
        DST_FUNC_LT_MEDIUM_WINK = 0x114D,
        DST_CALLWAITING_BTN_PRS = 0x114E,
        DST_CALLWAITING_BTN_RLS = 0x114F,
        DST_PARK_BTN_PRS = 0x1150,
        DST_PARK_BTN_RLS = 0x1151,
        DST_NEWCALL_BTN_PRS = 0x1152,
        DST_NEWCALL_BTN_RLS = 0x1153,
        DST_PARK_LT_OFF = 0x1154,
        DST_PARK_LT_ON = 0x1155,
        DST_PARK_LT_FLASHING = 0x1156,
        DST_PARK_LT_FASTFLASHING = 0x1157,
        DST_PARK_LT_VERY_FASTFLASHING = 0x1158,
        DST_PARK_LT_QUICKFLASH = 0x1159,
        DST_PARK_LT_WINK = 0x115A,
        DST_PARK_LT_SLOW_WINK = 0x115B,
        DST_PARK_LT_MEDIUM_WINK = 0x115C,
        DST_SCROLL_BTN_PRS = 0x115D,
        DST_SCROLL_BTN_RLS = 0x115E,
        DST_DIVERT_BTN_PRS = 0x115F,
        DST_DIVERT_BTN_RLS = 0x1160,
        DST_GROUP_BTN_PRS = 0x1161,
        DST_GROUP_BTN_RLS = 0x1162,
        DST_SPEEDDIAL_BTN_PRS = 0x1163,
        DST_SPEEDDIAL_BTN_RLS = 0x1164,
        DST_DND_BTN_PRS = 0x1165,
        DST_DND_BTN_RLS = 0x1166,
        DST_ENTER_BTN_PRS = 0x1167,
        DST_ENTER_BTN_RLS = 0x1168,
        DST_CLEAR_BTN_PRS = 0x1169,
        DST_CLEAR_BTN_RLS = 0x116A,
        DST_DESTINATION_BTN_PRS = 0x116B,
        DST_DESTINATION_BTN_RLS = 0x116C,
        DST_DND_LT_OFF = 0x116D,
        DST_DND_LT_ON = 0x116E,
        DST_DND_LT_FLASHING = 0x116F,
        DST_DND_LT_FASTFLASHING = 0x1170,
        DST_DND_LT_VERY_FASTFLASHING = 0x1171,
        DST_DND_LT_QUICKFLASH = 0x1172,
        DST_DND_LT_WINK = 0x1173,
        DST_DND_LT_SLOW_WINK = 0x1174,
        DST_DND_LT_MEDIUM_WINK = 0x1175,
        DST_GROUP_LT_OFF = 0x1176,
        DST_GROUP_LT_ON = 0x1177,
        DST_GROUP_LT_FLASHING = 0x1178,
        DST_GROUP_LT_FASTFLASHING = 0x1179,
        DST_GROUP_LT_VERY_FASTFLASHING = 0x117A,
        DST_GROUP_LT_QUICKFLASH = 0x117B,
        DST_GROUP_LT_WINK = 0x117C,
        DST_GROUP_LT_SLOW_WINK = 0x117D,
        DST_GROUP_LT_MEDIUM_WINK = 0x117E,
        DST_DIVERT_LT_OFF = 0x117F,
        DST_DIVERT_LT_ON = 0x1180,
        DST_DIVERT_LT_FLASHING = 0x1181,
        DST_DIVERT_LT_FASTFLASHING = 0x1182,
        DST_DIVERT_LT_VERY_FASTFLASHING = 0x1183,
        DST_DIVERT_LT_QUICKFLASH = 0x1184,
        DST_DIVERT_LT_WINK = 0x1185,
        DST_DIVERT_LT_SLOW_WINK = 0x1186,
        DST_DIVERT_LT_MEDIUM_WINK = 0x1187,
        DST_SCROLL_LT_OFF = 0x1188,
        DST_SCROLL_LT_ON = 0x1189,
        DST_SCROLL_LT_FLASHING = 0x118A,
        DST_SCROLL_LT_FASTFLASHING = 0x118B,
        DST_SCROLL_LT_VERY_FASTFLASHING = 0x118C,
        DST_SCROLL_LT_QUICKFLASH = 0x118D,
        DST_SCROLL_LT_WINK = 0x118E,
        DST_SCROLL_LT_SLOW_WINK = 0x118F,
        DST_SCROLL_LT_MEDIUM_WINK = 0x1190,
        DST_CALLBACK_BTN_PRS = 0x1191,
        DST_CALLBACK_BTN_RLS = 0x1192,
        DST_FLASH_LT_OFF = 0x1193,
        DST_FLASH_LT_ON = 0x1194,
        DST_FLASH_LT_FLASHING = 0x1195,
        DST_FLASH_LT_FASTFLASHING = 0x1196,
        DST_FLASH_LT_VERY_FASTFLASHING = 0x1197,
        DST_FLASH_LT_QUICKFLASH = 0x1198,
        DST_FLASH_LT_WINK = 0x1199,
        DST_FLASH_LT_SLOW_WINK = 0x119A,
        DST_FLASH_LT_MEDIUM_WINK = 0x119B,
        DST_MODE_BTN_PRS = 0x119C,
        DST_MODE_BTN_RLS = 0x119D,
        DST_SPEAKER_LT_MEDIUM_WINK = 0x119E,
        DST_MSG_LT_MEDIUM_WINK = 0x119F,
        DST_SPEEDDIAL_LT_OFF = 0x11A0,
        DST_SPEEDDIAL_LT_ON = 0x11A1,
        DST_SPEEDDIAL_LT_FLASHING = 0x11A2,
        DST_SPEEDDIAL_LT_FASTFLASHING = 0x11A3,
        DST_SPEEDDIAL_LT_VERY_FASTFLASHING = 0x11A4,
        DST_SPEEDDIAL_LT_QUICKFLASH = 0x11A5,
        DST_SPEEDDIAL_LT_WINK = 0x11A6,
        DST_SPEEDDIAL_LT_SLOW_WINK = 0x11A7,
        DST_SPEEDDIAL_LT_MEDIUM_WINK = 0x11A8,
        DST_SELECT_BTN_PRS = 0x11A9,
        DST_SELECT_BTN_RLS = 0x11AA,
        DST_PAUSE_BTN_PRS = 0x11AB,
        DST_PAUSE_BTN_RLS = 0x11AC,
        DST_INTERCOM_BTN_PRS = 0x11AD,
        DST_INTERCOM_BTN_RLS = 0x11AE,
        DST_INTERCOM_LT_OFF = 0x11AF,
        DST_INTERCOM_LT_ON = 0x11B0,
        DST_INTERCOM_LT_FLASHING = 0x11B1,
        DST_INTERCOM_LT_FASTFLASHING = 0x11B2,
        DST_INTERCOM_LT_VERY_FASTFLASHING = 0x11B3,
        DST_INTERCOM_LT_QUICKFLASH = 0x11B4,
        DST_INTERCOM_LT_WINK = 0x11B5,
        DST_INTERCOM_LT_SLOW_WINK = 0x11B6,
        DST_INTERCOM_LT_MEDIUM_WINK = 0x11B7,
        DST_CFWD_LT_OFF = 0x11B8,
        DST_CFWD_LT_ON = 0x11B9,
        DST_CFWD_LT_FLASHING = 0x11BA,
        DST_CFWD_LT_FASTFLASHING = 0x11BB,
        DST_CFWD_LT_VERY_FASTFLASHING = 0x11BC,
        DST_CFWD_LT_QUICKFLASH = 0x11BD,
        DST_CFWD_LT_WINK = 0x11BE,
        DST_CFWD_LT_SLOW_WINK = 0x11BF,
        DST_CFWD_LT_MEDIUM_WINK = 0x11C0,
        DST_CFWD_BTN_PRS = 0x11C1,
        DST_CFWD_BTN_RLS = 0x11C2,
        DST_SPECIAL_LT_OFF = 0x11C3,
        DST_SPECIAL_LT_ON = 0x11C4,
        DST_SPECIAL_LT_FLASHING = 0x11C5,
        DST_SPECIAL_LT_FASTFLASHING = 0x11C6,
        DST_SPECIAL_LT_VERY_FASTFLASHING = 0x11C7,
        DST_SPECIAL_LT_QUICKFLASH = 0x11C8,
        DST_SPECIAL_LT_WINK = 0x11C9,
        DST_SPECIAL_LT_SLOW_WINK = 0x11CA,
        DST_SPECIAL_LT_MEDIUM_WINK = 0x11CB,
        DST_SPECIAL_BTN_PRS = 0x11CC,
        DST_SPECIAL_BTN_RLS = 0x11CD,
        DST_FORWARD_LT_OFF = 0x11CE,
        DST_FORWARD_LT_ON = 0x11CF,
        DST_FORWARD_LT_FLASHING = 0x11D0,
        DST_FORWARD_LT_FASTFLASHING = 0x11D1,
        DST_FORWARD_LT_VERY_FASTFLASHING = 0x11D2,
        DST_FORWARD_LT_QUICKFLASH = 0x11D3,
        DST_FORWARD_LT_WINK = 0x11D4,
        DST_FORWARD_LT_SLOW_WINK = 0x11D5,
        DST_FORWARD_LT_MEDIUM_WINK = 0x11D6,
        DST_FORWARD_BTN_PRS = 0x11D7,
        DST_FORWARD_BTN_RLS = 0x11D8,
        DST_OUTGOING_LT_OFF = 0x11D9,
        DST_OUTGOING_LT_ON = 0x11DA,
        DST_OUTGOING_LT_FLASHING = 0x11DB,
        DST_OUTGOING_LT_FASTFLASHING = 0x11DC,
        DST_OUTGOING_LT_VERY_FASTFLASHING = 0x11DD,
        DST_OUTGOING_LT_QUICKFLASH = 0x11DE,
        DST_OUTGOING_LT_WINK = 0x11DF,
        DST_OUTGOING_LT_SLOW_WINK = 0x11E0,
        DST_OUTGOING_LT_MEDIUM_WINK = 0x11E1,
        DST_OUTGOING_BTN_PRS = 0x11E2,
        DST_OUTGOING_BTN_RLS = 0x11E3,
        DST_BACKSPACE_LT_OFF = 0x11E4,
        DST_BACKSPACE_LT_ON = 0x11E5,
        DST_BACKSPACE_LT_FLASHING = 0x11E6,
        DST_BACKSPACE_LT_FASTFLASHING = 0x11E7,
        DST_BACKSPACE_LT_VERY_FASTFLASHING = 0x11E8,
        DST_BACKSPACE_LT_QUICKFLASH = 0x11E9,
        DST_BACKSPACE_LT_WINK = 0x11EA,
        DST_BACKSPACE_LT_SLOW_WINK = 0x11EB,
        DST_BACKSPACE_LT_MEDIUM_WINK = 0x11EC,
        DST_BACKSPACE_BTN_PRS = 0x11ED,
        DST_BACKSPACE_BTN_RLS = 0x11EE,
        DST_START_TONE = 0x11EF,
        DST_STOP_TONE = 0x11F0,
        DST_FLASHHOOK = 0x11F1,
        DST_LINE_BTN_RLS = 0x11F2,
        DST_FEATURE_BTN_RLS = 0x11F3,
        DST_MUTE_BTN_RLS = 0x11F4,
        DST_HELP_BTN_RLS = 0x11F5,
        DST_LOGON_BTN_RLS = 0x11F6,
        DST_ANSWER_BTN_RLS = 0x11F7,
        DST_PROGRAM_BTN_RLS = 0x11F8,
        DST_CONFERENCE_BTN_RLS = 0x11F9,
        DST_RECALL_BTN_RLS = 0x11FA,
        DST_BREAK_BTN_RLS = 0x11FB,
        DST_WORK_BTN_RLS = 0x11FC,
        DST_TALLY_BTN_RLS = 0x11FD,
        DST_EXPAND_LT_OFF = 0x1200,
        DST_EXPAND_LT_ON = 0x1201,
        DST_EXPAND_LT_FLASHING = 0x1202,
        DST_EXPAND_LT_FASTFLASHING = 0x1203,
        DST_EXPAND_LT_VERY_FASTFLASHING = 0x1204,
        DST_EXPAND_LT_QUICKFLASH = 0x1205,
        DST_EXPAND_LT_WINK = 0x1206,
        DST_EXPAND_LT_SLOW_WINK = 0x1207,
        DST_EXPAND_LT_MEDIUM_WINK = 0x1208,
        DST_EXPAND_BTN_PRS = 0x1209,
        DST_EXPAND_BTN_RLS = 0x120A,
        DST_SERVICES_LT_OFF = 0x1210,
        DST_SERVICES_LT_ON = 0x1211,
        DST_SERVICES_LT_FLASHING = 0x1212,
        DST_SERVICES_LT_FASTFLASHING = 0x1213,
        DST_SERVICES_LT_VERY_FASTFLASHING = 0x1214,
        DST_SERVICES_LT_QUICKFLASH = 0x1215,
        DST_SERVICES_LT_WINK = 0x1216,
        DST_SERVICES_LT_SLOW_WINK = 0x1217,
        DST_SERVICES_LT_MEDIUM_WINK = 0x1218,
        DST_SERVICES_BTN_PRS = 0x1219,
        DST_SERVICES_BTN_RLS = 0x121A,
        DST_HEADSET_LT_OFF = 0x1220,
        DST_HEADSET_LT_ON = 0x1221,
        DST_HEADSET_LT_FLASHING = 0x1222,
        DST_HEADSET_LT_FASTFLASHING = 0x1223,
        DST_HEADSET_LT_VERY_FASTFLASHING = 0x1224,
        DST_HEADSET_LT_QUICKFLASH = 0x1225,
        DST_HEADSET_LT_WINK = 0x1226,
        DST_HEADSET_LT_SLOW_WINK = 0x1227,
        DST_HEADSET_LT_MEDIUM_WINK = 0x1228,
        DST_HEADSET_BTN_PRS = 0x1229,
        DST_HEADSET_BTN_RLS = 0x122A,
        DST_NAVIGATION_BTN_PRS = 0x1239,
        DST_NAVIGATION_BTN_RLS = 0x123A,
        DST_COPY_LT_OFF = 0x1240,
        DST_COPY_LT_ON = 0x1241,
        DST_COPY_LT_FLASHING = 0x1242,
        DST_COPY_LT_FASTFLASHING = 0x1243,
        DST_COPY_LT_VERY_FASTFLASHING = 0x1244,
        DST_COPY_LT_QUICKFLASH = 0x1245,
        DST_COPY_LT_WINK = 0x1246,
        DST_COPY_LT_SLOW_WINK = 0x1247,
        DST_COPY_LT_MEDIUM_WINK = 0x1248,
        DST_COPY_BTN_PRS = 0x1249,
        DST_COPY_BTN_RLS = 0x124A,
        DST_LINE_LT_MEDIUM_WINK = 0x1250,
        DST_MIC_LT_MEDIUM_WINK = 0x1251,
        DST_HOLD_LT_MEDIUM_WINK = 0x1252,
        DST_RELEASE_LT_MEDIUM_WINK = 0x1253,
        DST_HELP_LT_MEDIUM_WINK = 0x1254,
        DST_SUPERVISOR_LT_MEDIUM_WINK = 0x1255,
        DST_READY_LT_MEDIUM_WINK = 0x1256,
        DST_LOGON_LT_MEDIUM_WINK = 0x1257,
        DST_WRAPUP_LT_MEDIUM_WINK = 0x1258,
        DST_RING_LT_MEDIUM_WINK = 0x1259,
        DST_ANSWER_LT_MEDIUM_WINK = 0x125A,
        DST_PROGRAM_LT_SLOW_WINK = 0x125B,
        DST_TRANSFER_LT_SLOW_WINK = 0x125C,
        DST_CONFERENCE_LT_SLOW_WINK = 0x125D,
        DST_SOFT_LT_MEDIUM_WINK = 0x125E,
        DST_MENU_LT_MEDIUM_WINK = 0x125F,
        DST_CALLWAITING_LT_MEDIUM_WINK = 0x1260,
        DST_REDIAL_LT_MEDIUM_WINK = 0x1261,
        DST_PAGE_LT_MEDIUM_WINK = 0x1262,
        DST_FEATURE_LT_MEDIUM_WINK = 0x1263,
        DST_PAGE_LT_WINK = 0x1264,
        DST_PAGE_LT_SLOW_WINK = 0x1265,
        DST_CALLBACK_LT_ON = 0x1267,
        DST_CALLBACK_LT_FLASHING = 0x1268,
        DST_CALLBACK_LT_WINK = 0x1269,
        DST_CALLBACK_LT_FASTFLASHING = 0x126a,
        DST_ICM_LT_OFF = 0x126b,
        DST_ICM_LT_ON = 0x126c,
        DST_ICM_LT_FLASHING = 0x126d,
        DST_ICM_LT_WINK = 0x126e,
        DST_ICM_LT_FASTFLASHING = 0x126f,
        DST_ICM_BTN_PRS = 0x1270,
        DST_ICM_BTN_RLS = 0x1271,
        DST_CISCO_SCCP_CALL_INFO = 0x1280,
        DST_CALLBACK_LT_OFF = 0x1266,
        DST_CONFERENCE_BTN_PRS = 0x1018,
        DST_FUNC_LT_FASTFLASHING = 0x100b,
        DST_FUNC_LT_FLASHING = 0x1003,
        DST_FUNC_LT_OFF = 0x1002,
        DST_FUNC_LT_ON = 0x1001,
        DST_FUNC_LT_QUICKFLASH = 0x1025,
        DST_FUNC_LT_VERY_FASTFLASHING = 0x1033,
        DST_DC_BTN_PRS = 0x1301,
        DST_LND_BTN_PRS = 0x1302,
        DST_CHK_BTN_PRS = 0x1303,

        DST_CALL_IN_PROGRESS = 0x6b,
        DST_CALL_ALERTING = 0x6e,
        DST_CALL_CONNECTED = 0x6f,
        DST_CALL_RELEASED = 0x70,
        DST_CALL_SUSPENDED = 0x71,
        DST_CALL_RESUMED = 0x72,
        DST_CALL_HELD = 0x73,
        DST_CALL_RETRIEVED = 0x74,
        DST_CALL_ABANDONED = 0x75,
        DST_CALL_REJECTED = 0x76,
    }

    // 事件码占用16bytes，采用顺序编码（从0开始编码）。
    // 若采用windows消息机制，windows消息编码：事件码+0x7000(WM_USER)
    public enum EventCode {
        //语音识别
        E_PROC_Recognize,	//0x0000	//语音识别结束事件

        //ISDN呼叫
        E_CHG_ISDNStatus,	//0x0001	//ISDN底层状态改变事件

        //ss7
        E_RCV_Ss7Msu,		//0x0002	//SS7 MSU接收通知事件
        E_CHG_Mtp3State,	//0x0003	//Mtp3状态改变事件

        //传真
        E_CHG_FaxChState,	//0x0004	//传真通道传真状态改变事件
        E_CHG_FaxPages,		//0x0005	//传真接收/发送页结束事件
        E_PROC_FaxEnd,		//0x0006	//传真结束事件

        //PCM线路同步状态
        E_CHG_PcmLinkStatus,//0x0007	//PCM线路同步状态改变事件


        //录音通道
        E_CHG_LineVoltage,	//0x0008	//录音通道线路电压变化事件


        //ss1
        E_RCV_CAS,		//0x0009	//接收到的CAS值有变化
        E_RCV_R2,		//0x000A	//收到新的R2 值


        //DTMF接收
        E_PROC_WaitDTMF,	//0x000B	//WaitDTMF任务结束事件
        E_CHG_RcvDTMF,		//0x000C

        //DTMF发送
        E_PROC_SendDTMF,	//0x000D	//发送DTMF任务结束事件


        //发送闪断
        E_PROC_SendFlash,	//0x000E	//发送闪断任务结束事件


        //放音
        E_PROC_PlayEnd,		//0x000F	//放音任务结束
        E_PROC_PlayFile,	//0x0010	//文件放音进程指示
        E_PROC_PlayFileList,//0x0011	//文件列表放音进程指示


        E_PROC_PlayMem,		//0x0012	//内存放音进程指示

        //录音
        E_PROC_RecordEnd,	//0x0013	//录音任务结束
        E_PROC_RecordFile,	//0x0014	//文件录音任务进展指示
        E_PROC_RecordMem,	//0x0015	//内存录音任务进展指示


        //FSK发送
        E_PROC_SendFSK,		//0x0016	//发送FSK任务结束事件

        //FSK接收
        E_PROC_RcvFSK,		//0x0017	//RcvFSK任务结束


        //呼叫控制
        E_CHG_ChState,		//0x0018	//通道状态发生变化
        E_PROC_AutoDial,	//0x0019	//AutoDial任务有进展
        E_CHG_RemoteChBlock,//0x001A
        E_CHG_RemotePCMBlock,//0x001B
        E_SYS_ActualPickup,	//0x001C	//外线通道实际摘机
        E_CHG_RingFlag,		//0x001D	//铃流电平变化
        E_CHG_RingCount,	//0x001E	//振铃计数变化
        E_CHG_CIDExBuf,		//0x001F	//CID扩展接收缓冲区变化
        E_CHG_RxPhoNumBuf,	//0x0020	//被叫号码接收缓冲区变化
        E_CHG_PolarRvrsCount,//0x0021	//外线通道极性反转
        E_SYS_RemotePickup,	//0x0022	//模拟电话线相对能量算法检测被叫摘机

        //座席
        E_CHG_FlashCount,	//0x0023	//flash计数发生变化
        E_CHG_HookState,	//0x0024	//Hook状态发生变化

        //信号音检测
        E_CHG_ToneAnalyze,	//0x0025	//信号音分析结果变化事件
        E_OverallEnergy,	//0x0026
        E_CHG_OvrlEnrgLevel,//0x0027	//全频能量标识输出事件
        E_CHG_BusyTone,		//0x0028	//忙音计数变化事件
        E_CHG_BusyToneEx,	//0x0029	//松散忙音变化
        E_CHG_VocFxFlag,	//0x002A	//单音频信号音电平变化
        E_CHG_ToneValue,	//0x002B	//信号音电平变化
        E_CHG_RingEchoToneTime,	//0x002C
        E_CHG_PeakFrq,		//0x002D	//PeakFrq有变化
        E_SYS_BargeIn,		//0x002E	//检测到BargeIn
        E_SYS_NoSound,		//0x002F	//检测到NoSound

        //定时器
        E_SYS_TIMEOUT,		//0x0030	//定时器事件

        //信令监控
        E_CHG_SpyState,		//0x0031	//被监控电路的接续状态通知事件
        E_CHG_SpyLinkStatus,//0x0032	//被监控的PCM链路状态通知事件

        //数字电话录音卡
        E_RCV_DTR_AUDIO,	//0x0033	//语音通道开关事件
        E_RCV_DTR_HOOK,		//0x0034	//摘挂机事件
        E_RCV_DTR_LAMP,		//0x0035	//灯状态变化事件
        E_RCV_DTR_FKEY,		//0x0036	//Function Key 事件
        E_RCV_DTR_DKEY,		//0x0037	//Dial Key 事件
        E_RCV_DTR_VOX,		//0x0038	//VOX开关事件
        E_RCV_DTR_DISPLAY,	//0x0039	//显示事件
        E_RCV_DTR_DIRECTION,//0x003a	//呼叫方向事件
        E_RCV_DTR_RING,		//0x003b	//振铃事件

        E_CHG_CICRxPhoNumBuf = 0x003c,
        E_CHG_CICState = 0x003d,
        E_PROC_CICAutoDial = 0x003e,
        E_RCV_Ss7IsupUtuinf = 0x003f,
        E_CHG_Mtp2Status = 0x0040,
        E_RCV_DSTDChannel = 0x0041,
        E_RCV_Ss7SpyMsu = 0X0042,
        E_CHG_ToneDetector = 0x0043,
        E_CHG_ToneDetectorItem = 0x0044,
        E_RCV_CALLERID = 0x0045,
        E_PROC_FaxDcnTag = 0x0046,
        E_CHG_AMD = 0x0047,
        E_RCV_Ss7IsupCpg = 0x0048,
        E_CHG_CbChStatus = 0x0049,
        E_REFER_Status = 0x0050,
        E_CHG_SpyHangupInfo = 0x0051,
        E_CHG_CallBackRingCount = 0x0052,
        //Reserved 0x53-0x5f
        //+++start+++	added by wangfeng for synIPR, 2011.07.12
        E_RCV_IPR_DChannel = 0x0060,
        E_RCV_IPR_DONGLE_ADDED = 0x0061,
        E_RCV_IPR_DONGLE_REMOVED = 0x0062,
        E_RCV_IPR_NIC_LINKED = 0x0063,
        E_RCV_IPR_NIC_UNLINKED = 0x0064,
        E_RCV_IPR_AUTH_OVERFLOW = 0x0065,
        E_RCV_IPR_MEDIA_SESSION_STARTED = 0x0066,
        E_RCV_IPR_MEDIA_SESSION_STOPED = 0x0067,
        E_RCV_IPR_AUX_MEDIA_SESSION_STARTED = 0x0068,
        E_RCV_IPR_MEDIA_SESSION_FOWARDING = 0x0069,
        E_RCV_IPR_MEDIA_SESSION_FOWARD_STOPED = 0x006a,
        E_RCV_IPR_STATION_ADDED = 0x006b,
        E_RCV_IPR_STATION_REMOVED = 0x006c,
        //+++ end +++	added by wangfeng for synIPR, 2011.07.12
        //+++start+++ added by netwolf for SynIPR,2011.07.22
        E_IPR_LINK_REC_SLAVER_CONNECTED = 0x006d,
        E_IPR_LINK_REC_SLAVER_DISCONNECTED = 0x006e,
        E_IPR_SLAVER_INIT_CB = 0x006f,
        E_IPR_ACTIVE_SESSION_CB = 0x0070,
        E_IPR_DEACTIVE_SESSION_CB = 0x0071,
        E_IPR_START_REC_CB = 0x0072,
        E_IPR_STOP_REC_CB = 0x0073,
        E_IPR_PAUSE_REC_CB = 0x0074,
        E_IPR_RESTART_REC_CB = 0x0075,
        E_IPR_START_SLAVER_CB = 0x0076,
        E_IPR_CLOSE_SLAVER_CB = 0x0077,
        E_IPR_RCV_DTMF = 0x0078,
        E_IPR_ACTIVE_AND_REC_CB = 0x0079,
        E_IPR_DEACTIVE_AND_STOPREC_CB = 0x007a,
        //+++ end +++ added by netwolf for SynIPR,2011.07.22

        //+++start+++ ADD BY CYQ FOR SynIPR,2011.09.28
        E_RCV_IPA_DONGLE_ADDED = 0x007b,
        E_RCV_IPA_DONGLE_REMOVED = 0x007c,
        //+++ end +++ ADD BY CYQ FOR SynIPR,2011.09.28
        E_RCV_IPA_APPLICATION_PENDING = 0x007d,
        E_RCV_IPR_AUX_MEDIA_SESSION_STOPED = 0x007e,
        E_BOARD_ICMP_CHANGE = 0x007f,


        E_RCV_IsdnSpyMsu = 0X0080,  // added by xzw for OS-5112
        E_RCV_DecodeSs7Msu = 0x0081,    // added by xzw for OS-5038
        E_CHG_RCV_SELCALL = 0x0082, // Channel Rcv Selcall Tone
        E_RCV_IsdnL2SpyMsu = 0X0083,    // added by gaoyong for DS-33733

        E_CHG_AMD_TIME = 0x0084,
        E_REG_OPTIONS_RESPONSE = 0x0085,   //DS-28518 2014.5.4 jince                  
        E_RCV_HMP_DONGLE_ADDED = 0x0086,    // HMP series: USB-key detected
        E_RCV_HMP_DONGLE_REMOVED = 0x0087,  // HMP series: the removal of USB-key detected
        E_REG_REQUEST = 0x0088,   //注册请求事件 added by xzw for sipserver
        E_REG_REGSTATUS = 0x0089,   //注册状态变更事件 added by xzw for sipserver
        E_RCV_REFER = 0x008A,   //呼叫转移事件 added by xzw for sipserver
        MAX_EVENT_SIZE
    };

    public enum DEventCode {
        DE_OFFHOOK = 0x8,
        DE_ONHOOK = 0xe,
        DE_LT_ON = 0x1001,
        DE_LT_OFF = 0x1002,
        DE_LT_FLASHING = 0x1003,
        DE_DGT_PRS = 0x1006,
        DE_DGT_RLS = 0x1007,
        DE_MSG_CHG = 0x1008,
        DE_STARTSTOP_ON = 0x1009,
        DE_STARTSTOP_OFF = 0x100a,
        DE_LT_FASTFLASHING = 0x100b,
        DE_DOWNLOAD_STATUS = 0x100c,
        DE_FINISHED_PLAY = 0x100d,
        DE_FUNC_BTN_PRS = 0x100e,
        DE_FUNC_BTN_RLS = 0x100f,
        DE_HOLD_BTN_PRS = 0x1010,
        DE_HOLD_BTN_RLS = 0x1011,
        DE_RELEASE_BTN_PRS = 0x1012,
        DE_RELEASE_BTN_RLS = 0x1013,
        DE_TRANSFER_BTN_PRS = 0x1014,
        DE_ANSWER_BTN_PRS = 0x1015,
        DE_SPEAKER_BTN_PRS = 0x1016,
        DE_REDIAL_BTN_PRS = 0x1017,
        DE_CONF_BTN_PRS = 0x1018,
        DE_RECALL_BTN_PRS = 0x1019,
        DE_FEATURE_BTN_PRS = 0x101a,
        DE_UP_DOWN = 0x101b,
        DE_EXIT_BTN_PRS = 0x101c,
        DE_HELP_BTN_PRS = 0x101d,
        DE_SOFT_BTN_PRS = 0x101e,
        DE_RING_ON = 0x101f,
        DE_RING_OFF = 0x1020,
        DE_LINE_BTN_PRS = 0x1021,
        DE_MENU_BTN_PRS = 0x1022,
        DE_PREVIOUS_BTN_PRS = 0x1023,
        DE_NEXT_BTN_PRS = 0x1024,
        DE_LT_QUICKFLASH = 0x1025,
        DE_AUDIO_ON = 0x1026,
        DE_AUDIO_OFF = 0x1027,
        DE_DISPLAY_CLOCK = 0x1028,
        DE_DISPLAY_TIMER = 0x1029,
        DE_DISPLAY_CLEAR = 0x102a,
        DE_CFWD = 0x102b,
        DE_CFWD_CANCELED = 0x102c,
        DE_AUTO_ANSWER = 0x102d,
        DE_AUTO_ANSWER_CANCELED = 0x102e,
        DE_SET_BUSY = 0x102f,
        DE_SET_BUSY_CANCELED = 0x1030,
        DE_DESTINATION_BUSY = 0x1031,
        DE_REORDER = 0x1032,
        DE_LT_VERY_FASTFLASHING = 0x1033,
        DE_SPEAKER_BTN_RLS = 0x1034,
        DE_REDIAL_BTN_RLS = 0x1035,
        DE_TRANSFER_BTN_RLS = 0x1036,
        DE_CONF_BTN_RLS = 0x1037,
        DE_DISCONNECTED = 0x1038,
        DE_CONNECTED = 0x1039,
        DE_ABANDONED = 0x103a,
        DE_SUSPENDED = 0x103b,
        DE_RESUMED = 0x103c,
        DE_HELD = 0x103d,
        DE_RETRIEVED = 0x103e,
        DE_REJECTED = 0x103f,
        DE_MSG_BTN_PRS = 0x1040,
        DE_MSG_BTN_RLS = 0x1041,
        DE_SUPERVISOR_BTN_PRS = 0x1042,
        DE_SUPERVISOR_BTN_RLS = 0x1043,
        DE_WRAPUP_BTN_PRS = 0x1044,
        DE_WRAPUP_BTN_RLS = 0x1045,
        DE_READY_BTN_PRS = 0x1046,
        DE_READY_BTN_RLS = 0x1047,
        DE_LOGON_BTN_PRS = 0x1048,
        DE_BREAK_BTN_PRS = 0x1049,
        DE_AUDIO_CHG = 0x104a,
        DE_DISPLAY_MSG = 0x104b,
        DE_WORK_BTN_PRS = 0x104c,
        DE_TALLY_BTN_PRS = 0x104d,
        DE_PROGRAM_BTN_PRS = 0x104e,
        DE_MUTE_BTN_PRS = 0x104f,
        DE_ALERTING_AUTO_ANSWER = 0x1050,
        DE_MENU_BTN_RLS = 0x1051,
        DE_EXIT_BTN_RLS = 0x1052,
        DE_NEXT_BTN_RLS = 0x1053,
        DE_PREVIOUS_BTN_RLS = 0x1054,
        DE_SHIFT_BTN_PRS = 0x1055,
        DE_SHIFT_BTN_RLS = 0x1056,
        DE_PAGE_BTN_PRS = 0x1057,
        DE_PAGE_BTN_RLS = 0x1058,
        DE_SOFT_BTN_RLS = 0x1059,
        DE_LINE_LT_OFF = 0x1060,
        DE_LINE_LT_ON = 0x1061,
        DE_LINE_LT_FLASHING = 0x1062,
        DE_LINE_LT_FASTFLASHING = 0x1063,
        DE_LINE_LT_VERY_FASTFLASHING = 0x1064,
        DE_LINE_LT_QUICKFLASH = 0x1065,
        DE_LINE_LT_WINK = 0x1066,
        DE_LINE_LT_SLOW_WINK = 0x1067,
        DE_FEATURE_LT_OFF = 0x1068,
        DE_FEATURE_LT_ON = 0x1069,
        DE_FEATURE_LT_FLASHING = 0x106A,
        DE_FEATURE_LT_FASTFLASHING = 0x106B,
        DE_FEATURE_LT_VERY_FASTFLASHING = 0x106C,
        DE_FEATURE_LT_QUICKFLASH = 0x106D,
        DE_FEATURE_LT_WINK = 0x106E,
        DE_FEATURE_LT_SLOW_WINK = 0x106F,
        DE_SPEAKER_LT_OFF = 0x1070,
        DE_SPEAKER_LT_ON = 0x1071,
        DE_SPEAKER_LT_FLASHING = 0x1072,
        DE_SPEAKER_LT_FASTFLASHING = 0x1073,
        DE_SPEAKER_LT_VERY_FASTFLASHING = 0x1074,
        DE_SPEAKER_LT_QUICKFLASH = 0x1075,
        DE_SPEAKER_LT_WINK = 0x1076,
        DE_SPEAKER_LT_SLOW_WINK = 0x1077,
        DE_MIC_LT_OFF = 0x1078,
        DE_MIC_LT_ON = 0x1079,
        DE_MIC_LT_FLASHING = 0x107A,
        DE_MIC_LT_FASTFLASHING = 0x107B,
        DE_MIC_LT_VERY_FASTFLASHING = 0x107C,
        DE_MIC_LT_QUICKFLASH = 0x107D,
        DE_MIC_LT_WINK = 0x107E,
        DE_MIC_LT_SLOW_WINK = 0x107F,
        DE_HOLD_LT_OFF = 0x1080,
        DE_HOLD_LT_ON = 0x1081,
        DE_HOLD_LT_FLASHING = 0x1082,
        DE_HOLD_LT_FASTFLASHING = 0x1083,
        DE_HOLD_LT_VERY_FASTFLASHING = 0x1084,
        DE_HOLD_LT_QUICKFLASH = 0x1085,
        DE_HOLD_LT_WINK = 0x1086,
        DE_HOLD_LT_SLOW_WINK = 0x1087,
        DE_RELEASE_LT_OFF = 0x1088,
        DE_RELEASE_LT_ON = 0x1089,
        DE_RELEASE_LT_FLASHING = 0x108A,
        DE_RELEASE_LT_FASTFLASHING = 0x108B,
        DE_RELEASE_LT_VERY_FASTFLASHING = 0x108C,
        DE_RELEASE_LT_QUICKFLASH = 0x108D,
        DE_RELEASE_LT_WINK = 0x108E,
        DE_RELEASE_LT_SLOW_WINK = 0x108F,
        DE_HELP_LT_OFF = 0x1090,
        DE_HELP_LT_ON = 0x1091,
        DE_HELP_LT_FLASHING = 0x1092,
        DE_HELP_LT_FASTFLASHING = 0x1093,
        DE_HELP_LT_VERY_FASTFLASHING = 0x1094,
        DE_HELP_LT_QUICKFLASH = 0x1095,
        DE_HELP_LT_WINK = 0x1096,
        DE_HELP_LT_SLOW_WINK = 0x1097,
        DE_SUPERVISOR_LT_OFF = 0x1098,
        DE_SUPERVISOR_LT_ON = 0x1099,
        DE_SUPERVISOR_LT_FLASHING = 0x109A,
        DE_SUPERVISOR_LT_FASTFLASHING = 0x109B,
        DE_SUPERVISOR_LT_VERY_FASTFLASHING = 0x109C,
        DE_SUPERVISOR_LT_QUICKFLASH = 0x109D,
        DE_SUPERVISOR_LT_WINK = 0x109E,
        DE_SUPERVISOR_LT_SLOW_WINK = 0x109F,
        DE_READY_LT_OFF = 0x10A0,
        DE_READY_LT_ON = 0x10A1,
        DE_READY_LT_FLASHING = 0x10A2,
        DE_READY_LT_FASTFLASHING = 0x10A3,
        DE_READY_LT_VERY_FASTFLASHING = 0x10A4,
        DE_READY_LT_QUICKFLASH = 0x10A5,
        DE_READY_LT_WINK = 0x10A6,
        DE_READY_LT_SLOW_WINK = 0x10A7,
        DE_LOGON_LT_OFF = 0x10A8,
        DE_LOGON_LT_ON = 0x10A9,
        DE_LOGON_LT_FLASHING = 0x10AA,
        DE_LOGON_LT_FASTFLASHING = 0x10AB,
        DE_LOGON_LT_VERY_FASTFLASHING = 0x10AC,
        DE_LOGON_LT_QUICKFLASH = 0x10AD,
        DE_LOGON_LT_WINK = 0x10AE,
        DE_LOGON_LT_SLOW_WINK = 0x10AF,
        DE_WRAPUP_LT_OFF = 0x10B0,
        DE_WRAPUP_LT_ON = 0x10B1,
        DE_WRAPUP_LT_FLASHING = 0x10B2,
        DE_WRAPUP_LT_FASTFLASHING = 0x10B3,
        DE_WRAPUP_LT_VERY_FASTFLASHING = 0x10B4,
        DE_WRAPUP_LT_QUICKFLASH = 0x10B5,
        DE_WRAPUP_LT_WINK = 0x10B6,
        DE_WRAPUP_LT_SLOW_WINK = 0x10B7,
        DE_RING_LT_OFF = 0x10B8,
        DE_RING_LT_ON = 0x10B9,
        DE_RING_LT_FLASHING = 0x10BA,
        DE_RING_LT_FASTFLASHING = 0x10BB,
        DE_RING_LT_VERY_FASTFLASHING = 0x10BC,
        DE_RING_LT_QUICKFLASH = 0x10BD,
        DE_RING_LT_WINK = 0x10BE,
        DE_RING_LT_SLOW_WINK = 0x10BF,
        DE_ANSWER_LT_OFF = 0x10C0,
        DE_ANSWER_LT_ON = 0x10C1,
        DE_ANSWER_LT_FLASHING = 0x10C2,
        DE_ANSWER_LT_FASTFLASHING = 0x10C3,
        DE_ANSWER_LT_VERY_FASTFLASHING = 0x10C4,
        DE_ANSWER_LT_QUICKFLASH = 0x10C5,
        DE_ANSWER_LT_WINK = 0x10C6,
        DE_ANSWER_LT_SLOW_WINK = 0x10C7,
        DE_PROGRAM_LT_OFF = 0x10C8,
        DE_PROGRAM_LT_ON = 0x10C9,
        DE_PROGRAM_LT_FLASHING = 0x10CA,
        DE_PROGRAM_LT_FASTFLASHING = 0x10CB,
        DE_PROGRAM_LT_VERY_FASTFLASHING = 0x10CC,
        DE_PROGRAM_LT_QUICKFLASH = 0x10CD,
        DE_PROGRAM_LT_WINK = 0x10CE,
        DE_PROGRAM_LT_MEDIUM_WINK = 0x10CF,
        DE_MSG_LT_OFF = 0x10D0,
        DE_MSG_LT_ON = 0x10D1,
        DE_MSG_LT_FLASHING = 0x10D2,
        DE_MSG_LT_FASTFLASHING = 0x10D3,
        DE_MSG_LT_VERY_FASTFLASHING = 0x10D4,
        DE_MSG_LT_QUICKFLASH = 0x10D5,
        DE_MSG_LT_WINK = 0x10D6,
        DE_MSG_LT_SLOW_WINK = 0x10D7,
        DE_TRANSFER_LT_OFF = 0x10D8,
        DE_TRANSFER_LT_ON = 0x10D9,
        DE_TRANSFER_LT_FLASHING = 0x10DA,
        DE_TRANSFER_LT_FASTFLASHING = 0x10DB,
        DE_TRANSFER_LT_VERY_FASTFLASHING = 0x10DC,
        DE_TRANSFER_LT_QUICKFLASH = 0x10DD,
        DE_TRANSFER_LT_WINK = 0x10DE,
        DE_TRANSFER_LT_MEDIUM_WINK = 0x10DF,
        DE_CONFERENCE_LT_OFF = 0x10E0,
        DE_CONFERENCE_LT_ON = 0x10E1,
        DE_CONFERENCE_LT_FLASHING = 0x10E2,
        DE_CONFERENCE_LT_FASTFLASHING = 0x10E3,
        DE_CONFERENCE_LT_VERY_FASTFLASHING = 0x10E4,
        DE_CONFERENCE_LT_QUICKFLASH = 0x10E5,
        DE_CONFERENCE_LT_WINK = 0x10E6,
        DE_CONFERENCE_LT_MEDIUM_WINK = 0x10E7,
        DE_SOFT_LT_OFF = 0x10E8,
        DE_SOFT_LT_ON = 0x10E9,
        DE_SOFT_LT_FLASHING = 0x10EA,
        DE_SOFT_LT_FASTFLASHING = 0x10EB,
        DE_SOFT_LT_VERY_FASTFLASHING = 0x10EC,
        DE_SOFT_LT_QUICKFLASH = 0x10ED,
        DE_SOFT_LT_WINK = 0x10EE,
        DE_SOFT_LT_SLOW_WINK = 0x10EF,
        DE_MENU_LT_OFF = 0x10F0,
        DE_MENU_LT_ON = 0x10F1,
        DE_MENU_LT_FLASHING = 0x10F2,
        DE_MENU_LT_FASTFLASHING = 0x10F3,
        DE_MENU_LT_VERY_FASTFLASHING = 0x10F4,
        DE_MENU_LT_QUICKFLASH = 0x10F5,
        DE_MENU_LT_WINK = 0x10F6,
        DE_MENU_LT_SLOW_WINK = 0x10F7,
        DE_CALLWAITING_LT_OFF = 0x10F8,
        DE_CALLWAITING_LT_ON = 0x10F9,
        DE_CALLWAITING_LT_FLASHING = 0x10FA,
        DE_CALLWAITING_LT_FASTFLASHING = 0x10FB,
        DE_CALLWAITING_LT_VERY_FASTFLASHING = 0x10FC,
        DE_CALLWAITING_LT_QUICKFLASH = 0x10FD,
        DE_CALLWAITING_LT_WINK = 0x10FE,
        DE_CALLWAITING_LT_SLOW_WINK = 0x10FF,
        DE_REDIAL_LT_OFF = 0x1100,
        DE_REDIAL_LT_ON = 0x1101,
        DE_REDIAL_LT_FLASHING = 0x1102,
        DE_REDIAL_LT_FASTFLASHING = 0x1103,
        DE_REDIAL_LT_VERY_FASTFLASHING = 0x1104,
        DE_REDIAL_LT_QUICKFLASH = 0x1105,
        DE_REDIAL_LT_WINK = 0x1106,
        DE_REDIAL_LT_SLOW_WINK = 0x1107,
        DE_PAGE_LT_OFF = 0x1108,
        DE_PAGE_LT_ON = 0x1109,
        DE_PAGE_LT_FLASHING = 0x110A,
        DE_PAGE_LT_FASTFLASHING = 0x110B,
        DE_PAGE_LT_VERY_FASTFLASHING = 0x110C,
        DE_PAGE_LT_QUICKFLASH = 0x110D,
        DE_CTRL_BTN_PRS = 0x110E,
        DE_CTRL_BTN_RLS = 0x110F,
        DE_CANCEL_BTN_PRS = 0x1110,
        DE_CANCEL_BTN_RLS = 0x1111,
        DE_MIC_BTN_PRS = 0x1112,
        DE_MIC_BTN_RLS = 0x1113,
        DE_FLASH_BTN_PRS = 0x1114,
        DE_FLASH_BTN_RLS = 0x1115,
        DE_DIRECTORY_BTN_PRS = 0x1116,
        DE_DIRECTORY_BTN_RLS = 0x1117,
        DE_HANDSFREE_BTN_PRS = 0x1118,
        DE_HANDSFREE_BTN_RLS = 0x1119,
        DE_RINGTONE_BTN_PRS = 0x111A,
        DE_RINGTONE_BTN_RLS = 0x111B,
        DE_SAVE_BTN_PRS = 0x111C,
        DE_SAVE_BTN_RLS = 0x111D,
        DE_MUTE_LT_OFF = 0x111E,
        DE_MUTE_LT_ON = 0x111F,
        DE_MUTE_LT_FLASHING = 0x1120,
        DE_MUTE_LT_FASTFLASHING = 0x1121,
        DE_MUTE_LT_VERY_FASTFLASHING = 0x1122,
        DE_MUTE_LT_QUICKFLASH = 0x1123,
        DE_MUTE_LT_WINK = 0x1124,
        DE_MUTE_LT_SLOW_WINK = 0x1125,
        DE_MUTE_LT_MEDIUM_WINK = 0x1126,
        DE_HANDSFREE_LT_OFF = 0x1127,
        DE_HANDSFREE_LT_ON = 0x1128,
        DE_HANDSFREE_LT_FLASHING = 0x1129,
        DE_HANDSFREE_LT_FASTFLASHING = 0x112A,
        DE_HANDSFREE_LT_VERY_FASTFLASHING = 0x112B,
        DE_HANDSFREE_LT_QUICKFLASH = 0x112C,
        DE_HANDSFREE_LT_WINK = 0x112D,
        DE_HANDSFREE_LT_SLOW_WINK = 0x112E,
        DE_HANDSFREE_LT_MEDIUM_WINK = 0x112F,
        DE_DIRECTORY_LT_OFF = 0x1130,
        DE_DIRECTORY_LT_ON = 0x1131,
        DE_DIRECTORY_LT_FLASHING = 0x1132,
        DE_DIRECTORY_LT_FASTFLASHING = 0x1133,
        DE_DIRECTORY_LT_VERY_FASTFLASHING = 0x1134,
        DE_DIRECTORY_LT_QUICKFLASH = 0x1135,
        DE_DIRECTORY_LT_WINK = 0x1136,
        DE_DIRECTORY_LT_SLOW_WINK = 0x1137,
        DE_DIRECTORY_LT_MEDIUM_WINK = 0x1138,
        DE_RINGTONE_LT_OFF = 0x1139,
        DE_RINGTONE_LT_ON = 0x113A,
        DE_RINGTONE_LT_FLASHING = 0x113B,
        DE_RINGTONE_LT_FASTFLASHING = 0x113C,
        DE_RINGTONE_LT_VERY_FASTFLASHING = 0x113D,
        DE_RINGTONE_LT_QUICKFLASH = 0x113E,
        DE_RINGTONE_LT_WINK = 0x113F,
        DE_RINGTONE_LT_SLOW_WINK = 0x1140,
        DE_RINGTONE_LT_MEDIUM_WINK = 0x1141,
        DE_SAVE_LT_OFF = 0x1142,
        DE_SAVE_LT_ON = 0x1143,
        DE_SAVE_LT_FLASHING = 0x1144,
        DE_SAVE_LT_FASTFLASHING = 0x1145,
        DE_SAVE_LT_VERY_FASTFLASHING = 0x1146,
        DE_SAVE_LT_QUICKFLASH = 0x1147,
        DE_SAVE_LT_WINK = 0x1148,
        DE_SAVE_LT_SLOW_WINK = 0x1149,
        DE_SAVE_LT_MEDIUM_WINK = 0x114A,
        DE_FUNC_LT_WINK = 0x114B,
        DE_FUNC_LT_SLOW_WINK = 0x114C,
        DE_FUNC_LT_MEDIUM_WINK = 0x114D,
        DE_CALLWAITING_BTN_PRS = 0x114E,
        DE_CALLWAITING_BTN_RLS = 0x114F,
        DE_PARK_BTN_PRS = 0x1150,
        DE_PARK_BTN_RLS = 0x1151,
        DE_NEWCALL_BTN_PRS = 0x1152,
        DE_NEWCALL_BTN_RLS = 0x1153,
        DE_PARK_LT_OFF = 0x1154,
        DE_PARK_LT_ON = 0x1155,
        DE_PARK_LT_FLASHING = 0x1156,
        DE_PARK_LT_FASTFLASHING = 0x1157,
        DE_PARK_LT_VERY_FASTFLASHING = 0x1158,
        DE_PARK_LT_QUICKFLASH = 0x1159,
        DE_PARK_LT_WINK = 0x115A,
        DE_PARK_LT_SLOW_WINK = 0x115B,
        DE_PARK_LT_MEDIUM_WINK = 0x115C,
        DE_SCROLL_BTN_PRS = 0x115D,
        DE_SCROLL_BTN_RLS = 0x115E,
        DE_DIVERT_BTN_PRS = 0x115F,
        DE_DIVERT_BTN_RLS = 0x1160,
        DE_GROUP_BTN_PRS = 0x1161,
        DE_GROUP_BTN_RLS = 0x1162,
        DE_SPEEDDIAL_BTN_PRS = 0x1163,
        DE_SPEEDDIAL_BTN_RLS = 0x1164,
        DE_DND_BTN_PRS = 0x1165,
        DE_DND_BTN_RLS = 0x1166,
        DE_ENTER_BTN_PRS = 0x1167,
        DE_ENTER_BTN_RLS = 0x1168,
        DE_CLEAR_BTN_PRS = 0x1169,
        DE_CLEAR_BTN_RLS = 0x116A,
        DE_DESTINATION_BTN_PRS = 0x116B,
        DE_DESTINATION_BTN_RLS = 0x116C,
        DE_DND_LT_OFF = 0x116D,
        DE_DND_LT_ON = 0x116E,
        DE_DND_LT_FLASHING = 0x116F,
        DE_DND_LT_FASTFLASHING = 0x1170,
        DE_DND_LT_VERY_FASTFLASHING = 0x1171,
        DE_DND_LT_QUICKFLASH = 0x1172,
        DE_DND_LT_WINK = 0x1173,
        DE_DND_LT_SLOW_WINK = 0x1174,
        DE_DND_LT_MEDIUM_WINK = 0x1175,
        DE_GROUP_LT_OFF = 0x1176,
        DE_GROUP_LT_ON = 0x1177,
        DE_GROUP_LT_FLASHING = 0x1178,
        DE_GROUP_LT_FASTFLASHING = 0x1179,
        DE_GROUP_LT_VERY_FASTFLASHING = 0x117A,
        DE_GROUP_LT_QUICKFLASH = 0x117B,
        DE_GROUP_LT_WINK = 0x117C,
        DE_GROUP_LT_SLOW_WINK = 0x117D,
        DE_GROUP_LT_MEDIUM_WINK = 0x117E,
        DE_DIVERT_LT_OFF = 0x117F,
        DE_DIVERT_LT_ON = 0x1180,
        DE_DIVERT_LT_FLASHING = 0x1181,
        DE_DIVERT_LT_FASTFLASHING = 0x1182,
        DE_DIVERT_LT_VERY_FASTFLASHING = 0x1183,
        DE_DIVERT_LT_QUICKFLASH = 0x1184,
        DE_DIVERT_LT_WINK = 0x1185,
        DE_DIVERT_LT_SLOW_WINK = 0x1186,
        DE_DIVERT_LT_MEDIUM_WINK = 0x1187,
        DE_SCROLL_LT_OFF = 0x1188,
        DE_SCROLL_LT_ON = 0x1189,
        DE_SCROLL_LT_FLASHING = 0x118A,
        DE_SCROLL_LT_FASTFLASHING = 0x118B,
        DE_SCROLL_LT_VERY_FASTFLASHING = 0x118C,
        DE_SCROLL_LT_QUICKFLASH = 0x118D,
        DE_SCROLL_LT_WINK = 0x118E,
        DE_SCROLL_LT_SLOW_WINK = 0x118F,
        DE_SCROLL_LT_MEDIUM_WINK = 0x1190,
        DE_CALLBACK_BTN_PRS = 0x1191,
        DE_CALLBACK_BTN_RLS = 0x1192,
        DE_FLASH_LT_OFF = 0x1193,
        DE_FLASH_LT_ON = 0x1194,
        DE_FLASH_LT_FLASHING = 0x1195,
        DE_FLASH_LT_FASTFLASHING = 0x1196,
        DE_FLASH_LT_VERY_FASTFLASHING = 0x1197,
        DE_FLASH_LT_QUICKFLASH = 0x1198,
        DE_FLASH_LT_WINK = 0x1199,
        DE_FLASH_LT_SLOW_WINK = 0x119A,
        DE_FLASH_LT_MEDIUM_WINK = 0x119B,
        DE_MODE_BTN_PRS = 0x119C,
        DE_MODE_BTN_RLS = 0x119D,
        DE_SPEAKER_LT_MEDIUM_WINK = 0x119E,
        DE_MSG_LT_MEDIUM_WINK = 0x119F,
        DE_SPEEDDIAL_LT_OFF = 0x11A0,
        DE_SPEEDDIAL_LT_ON = 0x11A1,
        DE_SPEEDDIAL_LT_FLASHING = 0x11A2,
        DE_SPEEDDIAL_LT_FASTFLASHING = 0x11A3,
        DE_SPEEDDIAL_LT_VERY_FASTFLASHING = 0x11A4,
        DE_SPEEDDIAL_LT_QUICKFLASH = 0x11A5,
        DE_SPEEDDIAL_LT_WINK = 0x11A6,
        DE_SPEEDDIAL_LT_SLOW_WINK = 0x11A7,
        DE_SPEEDDIAL_LT_MEDIUM_WINK = 0x11A8,
        DE_SELECT_BTN_PRS = 0x11A9,
        DE_SELECT_BTN_RLS = 0x11AA,
        DE_PAUSE_BTN_PRS = 0x11AB,
        DE_PAUSE_BTN_RLS = 0x11AC,
        DE_INTERCOM_BTN_PRS = 0x11AD,
        DE_INTERCOM_BTN_RLS = 0x11AE,
        DE_INTERCOM_LT_OFF = 0x11AF,
        DE_INTERCOM_LT_ON = 0x11B0,
        DE_INTERCOM_LT_FLASHING = 0x11B1,
        DE_INTERCOM_LT_FASTFLASHING = 0x11B2,
        DE_INTERCOM_LT_VERY_FASTFLASHING = 0x11B3,
        DE_INTERCOM_LT_QUICKFLASH = 0x11B4,
        DE_INTERCOM_LT_WINK = 0x11B5,
        DE_INTERCOM_LT_SLOW_WINK = 0x11B6,
        DE_INTERCOM_LT_MEDIUM_WINK = 0x11B7,
        DE_CFWD_LT_OFF = 0x11B8,
        DE_CFWD_LT_ON = 0x11B9,
        DE_CFWD_LT_FLASHING = 0x11BA,
        DE_CFWD_LT_FASTFLASHING = 0x11BB,
        DE_CFWD_LT_VERY_FASTFLASHING = 0x11BC,
        DE_CFWD_LT_QUICKFLASH = 0x11BD,
        DE_CFWD_LT_WINK = 0x11BE,
        DE_CFWD_LT_SLOW_WINK = 0x11BF,
        DE_CFWD_LT_MEDIUM_WINK = 0x11C0,
        DE_CFWD_BTN_PRS = 0x11C1,
        DE_CFWD_BTN_RLS = 0x11C2,
        DE_SPECIAL_LT_OFF = 0x11C3,
        DE_SPECIAL_LT_ON = 0x11C4,
        DE_SPECIAL_LT_FLASHING = 0x11C5,
        DE_SPECIAL_LT_FASTFLASHING = 0x11C6,
        DE_SPECIAL_LT_VERY_FASTFLASHING = 0x11C7,
        DE_SPECIAL_LT_QUICKFLASH = 0x11C8,
        DE_SPECIAL_LT_WINK = 0x11C9,
        DE_SPECIAL_LT_SLOW_WINK = 0x11CA,
        DE_SPECIAL_LT_MEDIUM_WINK = 0x11CB,
        DE_SPECIAL_BTN_PRS = 0x11CC,
        DE_SPECIAL_BTN_RLS = 0x11CD,
        DE_FORWARD_LT_OFF = 0x11CE,
        DE_FORWARD_LT_ON = 0x11CF,
        DE_FORWARD_LT_FLASHING = 0x11D0,
        DE_FORWARD_LT_FASTFLASHING = 0x11D1,
        DE_FORWARD_LT_VERY_FASTFLASHING = 0x11D2,
        DE_FORWARD_LT_QUICKFLASH = 0x11D3,
        DE_FORWARD_LT_WINK = 0x11D4,
        DE_FORWARD_LT_SLOW_WINK = 0x11D5,
        DE_FORWARD_LT_MEDIUM_WINK = 0x11D6,
        DE_FORWARD_BTN_PRS = 0x11D7,
        DE_FORWARD_BTN_RLS = 0x11D8,
        DE_OUTGOING_LT_OFF = 0x11D9,
        DE_OUTGOING_LT_ON = 0x11DA,
        DE_OUTGOING_LT_FLASHING = 0x11DB,
        DE_OUTGOING_LT_FASTFLASHING = 0x11DC,
        DE_OUTGOING_LT_VERY_FASTFLASHING = 0x11DD,
        DE_OUTGOING_LT_QUICKFLASH = 0x11DE,
        DE_OUTGOING_LT_WINK = 0x11DF,
        DE_OUTGOING_LT_SLOW_WINK = 0x11E0,
        DE_OUTGOING_LT_MEDIUM_WINK = 0x11E1,
        DE_OUTGOING_BTN_PRS = 0x11E2,
        DE_OUTGOING_BTN_RLS = 0x11E3,
        DE_BACKSPACE_LT_OFF = 0x11E4,
        DE_BACKSPACE_LT_ON = 0x11E5,
        DE_BACKSPACE_LT_FLASHING = 0x11E6,
        DE_BACKSPACE_LT_FASTFLASHING = 0x11E7,
        DE_BACKSPACE_LT_VERY_FASTFLASHING = 0x11E8,
        DE_BACKSPACE_LT_QUICKFLASH = 0x11E9,
        DE_BACKSPACE_LT_WINK = 0x11EA,
        DE_BACKSPACE_LT_SLOW_WINK = 0x11EB,
        DE_BACKSPACE_LT_MEDIUM_WINK = 0x11EC,
        DE_BACKSPACE_BTN_PRS = 0x11ED,
        DE_BACKSPACE_BTN_RLS = 0x11EE,
        DE_START_TONE = 0x11EF,
        DE_STOP_TONE = 0x11F0,
        DE_FLASHHOOK = 0x11F1,
        DE_LINE_BTN_RLS = 0x11F2,
        DE_FEATURE_BTN_RLS = 0x11F3,
        DE_MUTE_BTN_RLS = 0x11F4,
        DE_HELP_BTN_RLS = 0x11F5,
        DE_LOGON_BTN_RLS = 0x11F6,
        DE_ANSWER_BTN_RLS = 0x11F7,
        DE_PROGRAM_BTN_RLS = 0x11F8,
        DE_CONFERENCE_BTN_RLS = 0x11F9,
        DE_RECALL_BTN_RLS = 0x11FA,
        DE_BREAK_BTN_RLS = 0x11FB,
        DE_WORK_BTN_RLS = 0x11FC,
        DE_TALLY_BTN_RLS = 0x11FD,
        DE_EXPAND_LT_OFF = 0x1200,
        DE_EXPAND_LT_ON = 0x1201,
        DE_EXPAND_LT_FLASHING = 0x1202,
        DE_EXPAND_LT_FASTFLASHING = 0x1203,
        DE_EXPAND_LT_VERY_FASTFLASHING = 0x1204,
        DE_EXPAND_LT_QUICKFLASH = 0x1205,
        DE_EXPAND_LT_WINK = 0x1206,
        DE_EXPAND_LT_SLOW_WINK = 0x1207,
        DE_EXPAND_LT_MEDIUM_WINK = 0x1208,
        DE_EXPAND_BTN_PRS = 0x1209,
        DE_EXPAND_BTN_RLS = 0x120A,
        DE_SERVICES_LT_OFF = 0x1210,
        DE_SERVICES_LT_ON = 0x1211,
        DE_SERVICES_LT_FLASHING = 0x1212,
        DE_SERVICES_LT_FASTFLASHING = 0x1213,
        DE_SERVICES_LT_VERY_FASTFLASHING = 0x1214,
        DE_SERVICES_LT_QUICKFLASH = 0x1215,
        DE_SERVICES_LT_WINK = 0x1216,
        DE_SERVICES_LT_SLOW_WINK = 0x1217,
        DE_SERVICES_LT_MEDIUM_WINK = 0x1218,
        DE_SERVICES_BTN_PRS = 0x1219,
        DE_SERVICES_BTN_RLS = 0x121A,
        DE_HEADSET_LT_OFF = 0x1220,
        DE_HEADSET_LT_ON = 0x1221,
        DE_HEADSET_LT_FLASHING = 0x1222,
        DE_HEADSET_LT_FASTFLASHING = 0x1223,
        DE_HEADSET_LT_VERY_FASTFLASHING = 0x1224,
        DE_HEADSET_LT_QUICKFLASH = 0x1225,
        DE_HEADSET_LT_WINK = 0x1226,
        DE_HEADSET_LT_SLOW_WINK = 0x1227,
        DE_HEADSET_LT_MEDIUM_WINK = 0x1228,
        DE_HEADSET_BTN_PRS = 0x1229,
        DE_HEADSET_BTN_RLS = 0x122A,
        DE_NAVIGATION_BTN_PRS = 0x1239,
        DE_NAVIGATION_BTN_RLS = 0x123A,
        DE_COPY_LT_OFF = 0x1240,
        DE_COPY_LT_ON = 0x1241,
        DE_COPY_LT_FLASHING = 0x1242,
        DE_COPY_LT_FASTFLASHING = 0x1243,
        DE_COPY_LT_VERY_FASTFLASHING = 0x1244,
        DE_COPY_LT_QUICKFLASH = 0x1245,
        DE_COPY_LT_WINK = 0x1246,
        DE_COPY_LT_SLOW_WINK = 0x1247,
        DE_COPY_LT_MEDIUM_WINK = 0x1248,
        DE_COPY_BTN_PRS = 0x1249,
        DE_COPY_BTN_RLS = 0x124A,
        DE_LINE_LT_MEDIUM_WINK = 0x1250,
        DE_MIC_LT_MEDIUM_WINK = 0x1251,
        DE_HOLD_LT_MEDIUM_WINK = 0x1252,
        DE_RELEASE_LT_MEDIUM_WINK = 0x1253,
        DE_HELP_LT_MEDIUM_WINK = 0x1254,
        DE_SUPERVISOR_LT_MEDIUM_WINK = 0x1255,
        DE_READY_LT_MEDIUM_WINK = 0x1256,
        DE_LOGON_LT_MEDIUM_WINK = 0x1257,
        DE_WRAPUP_LT_MEDIUM_WINK = 0x1258,
        DE_RING_LT_MEDIUM_WINK = 0x1259,
        DE_ANSWER_LT_MEDIUM_WINK = 0x125A,
        DE_PROGRAM_LT_SLOW_WINK = 0x125B,
        DE_TRANSFER_LT_SLOW_WINK = 0x125C,
        DE_CONFERENCE_LT_SLOW_WINK = 0x125D,
        DE_SOFT_LT_MEDIUM_WINK = 0x125E,
        DE_MENU_LT_MEDIUM_WINK = 0x125F,
        DE_CALLWAITING_LT_MEDIUM_WINK = 0x1260,
        DE_REDIAL_LT_MEDIUM_WINK = 0x1261,
        DE_PAGE_LT_MEDIUM_WINK = 0x1262,
        DE_FEATURE_LT_MEDIUM_WINK = 0x1263,
        DE_PAGE_LT_WINK = 0x1264,
        DE_PAGE_LT_SLOW_WINK = 0x1265,
        DE_CALLBACK_LT_ON = 0x1267,
        DE_CALLBACK_LT_FLASHING = 0x1268,
        DE_CALLBACK_LT_WINK = 0x1269,
        DE_CALLBACK_LT_FASTFLASHING = 0x126a,
        DE_ICM_LT_OFF = 0x126b,
        DE_ICM_LT_ON = 0x126c,
        DE_ICM_LT_FLASHING = 0x126d,
        DE_ICM_LT_WINK = 0x126e,
        DE_ICM_LT_FASTFLASHING = 0x126f,
        DE_ICM_BTN_PRS = 0x1270,
        DE_ICM_BTN_RLS = 0x1271,
        DE_CISCO_SCCP_CALL_INFO = 0x1280,
        DE_CALLBACK_LT_OFF = 0x1266,
        DE_CONFERENCE_BTN_PRS = DE_CONF_BTN_PRS,
        DE_FUNC_LT_FASTFLASHING = DE_LT_FASTFLASHING,
        DE_FUNC_LT_FLASHING = DE_LT_FLASHING,
        DE_FUNC_LT_OFF = DE_LT_OFF,
        DE_FUNC_LT_ON = DE_LT_ON,
        DE_FUNC_LT_QUICKFLASH = DE_LT_QUICKFLASH,
        DE_FUNC_LT_VERY_FASTFLASHING = DE_LT_VERY_FASTFLASHING,
        DE_DC_BTN_PRS = 0x1301,
        DE_LND_BTN_PRS = 0x1302,
        DE_CHK_BTN_PRS = 0x1303,

        DE_CALLSTATE_IDLE = 0x1304,
        DE_CALLSTATE_DIALING = 0x1306,
        DE_CALLSTATE_FAR_END_RINGBACK = 0x1308,
        DE_CALLSTATE_TALK = 0x1309,
        DE_SIP_RAW_MSG = 0x1400,
        DE_REMOTE_PARTYID = 0x1401,

        DE_CALL_IN_PROGRESS = 0x6b,
        DE_CALL_ALERTING = 0x6e,
        DE_CALL_CONNECTED = 0x6f,
        DE_CALL_RELEASED = 0x70,
        DE_CALL_SUSPENDED = 0x71,
        DE_CALL_RESUMED = 0x72,
        DE_CALL_HELD = 0x73,
        DE_CALL_RETRIEVED = 0x74,
        DE_CALL_ABANDONED = 0x75,
        DE_CALL_REJECTED = 0x76,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct tagBUS_OP {
        public int bEnHwOpBus;
        public int bEnHwOpSetLinkFromVlm;
        public int nST;
        public int nTs;
        public int nToBusCh;
        public int nPlayST;
        public int nPlayTs;
        public int nPlayToBusCh;
        public int nSpeakerVlm;
        public int nTotListener;
        public int[] pnListenerCh;
        public int nFromSpeaker;
        public int nDefaultSpeakerVlm;
        public int nTotChS;
        public int[] pnChS;
        public int nBindCh;
        public int nToBusChForVox;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MESSAGE_INFO {
        public ushort wEventCode;
        public int nReference;
        public uint dwParam; //输出参数
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_SET_INFO {
        public uint dwWorkMode;
        public IntPtr lpHandlerParam;
        public uint dwOutCondition;
        public uint dwOutParamVal;
        public uint dwUser;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_SET_INFO_CALLBACK {
        public uint dwWorkMode;
        public LPFNEVENTCALLBACK lpHandlerParam;
        public uint dwOutCondition;
        public uint dwOutParamVal;
        public uint dwUser;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EVENT_SET_INFO_CALLBACKA {
        public uint dwWorkMode;
        public LPFNEVENTCALLBACKA lpHandlerParam;
        public uint dwOutCondition;
        public uint dwOutParamVal;
        public uint dwUser;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct FAX_FILE_SCT {
        public string szFileName;	//no more than 256
        public int nStartPage;
        public int nEndPage;
        public int nReserve1;
        public int nReserve2;
    }

    //[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Ansi)]
    public struct SSM_EVENT {
        public ushort wEventCode;
        public int nReference;
        public uint dwParam;
        public uint dwUser;
        public uint dwSubReason;
        public uint dwXtraInfo;
        public IntPtr pvBuffer;
        public uint dwBufferLength;
        public uint dwDataLength;
        public uint dwEventFlag;//Falgs of the following:
        //bit 0,    =1 - App created the event
        //          =0 - SHP_A3.DLL created the event
        //bit 1,    Reserved
        //bit 2,    =1 - data has been truncated
        //          =0 - data has not been truncated
        public uint dwReserved1;
        public long llReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SSM_VERSION {
        public byte ucMajor;
        public byte ucMinor;
        public ushort usInternal;
        public ushort usBuild;
        public byte ucRelease;
        public byte ucFeature;
    }

    //[StructLayout(LayoutKind.Explicit, Size = 116, CharSet = CharSet.Ansi)]
    //public struct MediaParam
    //{
    //    [FieldOffset(0)]
    //    public int mode;
    //    [FieldOffset(4)]
    //    public char[] localIP;
    //    [FieldOffset(54)]
    //    public int localPort;
    //    [FieldOffset(58)]
    //    public char[] remoteIP;
    //    [FieldOffset(108)]
    //    public int remotePort;
    //    [FieldOffset(112)]
    //    public int sendCodecType;
    //}

    //SynIPR
    public struct S_un_b {
        public byte s_b1, s_b2, s_b3, s_b4; //char -> byte
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct S_IP_Addr {
        [FieldOffset(0)]
        public S_un_b S_un_b;
        [FieldOffset(0)]
        public uint S_addr;
    };

    public struct S_IP {
        public S_IP_Addr addr;
        public uint usPort;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IPR_Addr {
        [FieldOffset(0)]
        public Int64 nLLaddr;
        [FieldOffset(0)]
        public S_IP s_ip;
    }

    //IPRecorder Slaver Adress
    public struct IPR_SLAVERADDR {
        public int nRecSlaverID;
        public IPR_Addr ipAddr;
        public int nThreadPairs;
        public int nTotalResources;
        public int nUsedResources;
    };

    //session information
    public struct IPR_SessionInfo {
        public int nCallRef;			//call reference
        public int nStationId;			//one of the station of the session
        public int nStationId2;		    //another station of the session
        public uint dwSessionId;        //session Id
        public IPR_Addr PrimaryAddr;	//ip address and port of primary
        public int nPrimaryCodec;      //codec of primary
        public IPR_Addr SecondaryAddr;	//ip address and port of secondary
        public int nSecondaryCodec;	//codec of secondary
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] szpFowardingIp;	//forwarding ip for forward primary, null if no forwarding
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public char[] szsFowardingIp;	//forwarding ip for forward secondary, null if no forwarding
        public int nFowardingPPort;	//forwarding port for forward primary, -1 if no forwarding
        public int nFowardingSPort;	//forwarding port for forard secondary, -1 if no forwarding
    };

    //computer infomation
    public struct COMPUTER_INFO {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public char[] szOSVersion;			//version of OS
        public int nCPUNO;				//count of cpu
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] szCPUInfo;			//discription of cpu
        public uint ulPhysicalMemory;	    //physical memory
        public uint ulHardDisk;			//hard disk size
        public int nNetAdapterNO;			//count of adapter
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6 * 30)]
        public char[,] szMAC;		        //mac address
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6 * 30)]
        public char[,] szIPAddr;	        //ip address
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6 * 30)]
        public char[,] szMask;		        //mask
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6 * 30)]
        public char[,] szGateway;	        //gateway
    };

    //PBX Type
    public enum Protocol {
        PTL_SIP = 0,		    // Session Initiation Protocol
        PTL_CISCO_SKINNY = 1,   // Cisco SCCP Protocol
        PTL_AVAYA_H323 = 2,     // H323 Protocol
        PTL_SHORTEL_MGCP = 3,	// Shortel MGCP Protocol
        PTL_H323 = 4,			// H323 Protocol
        PTL_PANASONIC_MGCP = 5,	// Panasonic Protocol
        PTL_TOSHIBA_MEGACO = 6,	// Toshiba MEGACO Protocol
        PTL_MAX
    };

    public struct IPR_MONITOR_ITEM {
        public byte Protocol;    // Protocol type (i.e., MT_TCP or MT_UDP)
        public ushort Port;        // Port Number
    };

    public struct IPR_SCCP_CFGS {
        public IPR_MONITOR_ITEM SCCP;      // CISCO SCCP parameters
    };

    public struct IPR_H323_CFGS {
        public IPR_MONITOR_ITEM H225CS;
        public IPR_MONITOR_ITEM H225RAS;
        public ushort NonStationListCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        public uint[] NonStationList;
        public ushort H225CSAdditionalCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public IPR_MONITOR_ITEM[] H225CS_Additional;  // Optional H225 Call Signaling ports.
        public ushort H225RASAdditionalCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public IPR_MONITOR_ITEM[] H225RAS_Additional; // Optional H223 Registration ports.
    };

    public struct IPR_SIP_CFGS {
        public IPR_MONITOR_ITEM Transport;  // SIP IP Protocol Type and Port
        public uint ProxyIPAddress;        // SIP Proxy/ALG/PBX IP Address
        public ushort NonStationListCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public uint[] NonStationList;
        public ushort TransportAdditionalCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        public IPR_MONITOR_ITEM[] Transport_Additional;  // Optional SIP IP protocol type and ports.
    };

    public struct IPR_SHORTEL_MGCP_CFGS {
        public IPR_MONITOR_ITEM CallAgent;
        public IPR_MONITOR_ITEM Gateway;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct IPR_MONITOR_CFGS {
        [FieldOffset(0)]
        public IPR_SCCP_CFGS CISCO;                    // CISCO SCCP protocol parameters
        [FieldOffset(0)]
        public IPR_SIP_CFGS SIP;                       // SIP parameters
        [FieldOffset(0)]
        public IPR_H323_CFGS Avaya_H323;
        [FieldOffset(0)]
        public IPR_SHORTEL_MGCP_CFGS Shortel_MGCP;
        [FieldOffset(0)]
        public IPR_H323_CFGS H323;
    };

    //[StructLayout(LayoutKind.Sequential)]
    //call information
    public struct IPR_CALL_INFO {
        public uint CallRef;          //call reference
        public uint CallSource;       //call direction(incoming or outgoing)
        public uint Cause;            //cause
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] szCallerId;      //caller number or name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] szCalledId;      //called number or name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] szReferredBy;    //from which referred
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
        public char[] szReferTo;       //refer to which
    }
    /*
    [StructLayout(LayoutKind.Explicit, Size = 412, CharSet=CharSet.Ansi)] public struct IPR_CALL_INFO
    {
        [FieldOffset(0)] public UInt32 CallRef;
        [FieldOffset(4)] public UInt32 CallSource;
        [FieldOffset(8)] public UInt32 Cause;
        [FieldOffset(12)] public unsafe fixed char szCallerId[100];
        [FieldOffset(112)] public unsafe fixed char szCalledId[100];
        [FieldOffset(212)] public unsafe fixed char szReferredBy[100];
        [FieldOffset(312)] public unsafe fixed char szReferTo[100];
    }*/

    //SCCP call information
    //public struct IPR_CISCO_SCCP_CALL_INFO
    //{
    //public CallingPartyName
    //}
    public struct IPR_CISCO_SCCP_CALL_INFO {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public char[] CallingPartyName;					//calling party name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] CallingParty;					//calling party number
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public char[] CalledPartyName;					//called party name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] CalledParty;					//called party number
        public uint LineInstance;					//line instance
        public uint CallId;							//call index
        public uint CallType;						//call direction
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public char[] OrigCalledPartyName;			//original called party name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] OrigCalledParty;				//original called party number
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public char[] LastRedirectingPartyName;		//last redirecting party name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] LastRedirectingParty;			//last redirecting party number
        public uint OrigCalledPartyRedirectReason;	//original called party redirection reason
        public uint LastRedirectReason;				//last redirection reason
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] CallingPartyVoiceMailbox;		//calling party voice mailbox
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] CalledPartyVoiceMailbox;		//called party voice mailbox
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] OriginalCalledPartyVoiceMailbox;//original called party voice mailbox
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public char[] LastRedirectVoiceMailbox;		//last redirect voice mailbox
        public uint CallInstance;					//call instance
    };

    public struct IPR_DEVENT {
        public uint dwEventCode;
        public uint dwSubReason;
        public uint dwXtraInfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] pucBuffer;
        public uint dwDataLength;
    };

    //station list
    //[StructLayout(LayoutKind.Explicit, Size=8, CharSet=CharSet.Ansi)]
    public struct STATION_LIST {
        public int nStationId;			//station Id
        public byte ucCallCtrlId;		//call control signal Id
    };

    public enum TimerType {
        TIMER_ONE,
        TIMER_PERIODIC
    };

    public enum EventMode {
        NO_EVENT, // 无事件方式
        EVENT_POLLING, //事件轮询
        EVENT_CALLBACK, //事件回调
        EVENT_MESSAGE, //windows消息
        EVENT_POLLINGA, //事件轮询
        EVENT_CALLBACKA //事件回调
    };

    //文件放音进程指示输出参数类型.
    public enum PlayFileProc {
        PLAYPERCENT,
        PLAYTIME,
        DATABYTESPLAYED,
        DATABYTESTOPLAY
    };

    //内存录放音进程指示输出参数类型.
    public enum PlayMemProc {
        END_HALF_BUFFER,
        END_BUFFER,
        MEM_OFFSET,
        MEM_BYTES,
    };

    //文件录音进程指示输出参数类型.
    public enum RecFileProc {
        RECORD_TIME,
        RECORD_BYTES
    };

    //ISUP用户部分参数常量定义，函数SsmSetIsupFlag中nType类型
    public enum IsupSetFlag {
        ISUP_CallerParam = 1,//主叫号码参数
        ISUP_PhoNumParam = 2,//被叫号码参数
        ISUP_PhoNumREL = 3,//带号码改发信息的REL

        ISUP_REL_DENY_SetToOther = 100,//呼叫被拒设置为其它情况
    };

    public enum TupProc {
        Tup_ANX = 1,//呼叫：C_TUP_ANU, C_TUP_ANC, C_TUP_ANN
    };

    public delegate void RXDTMFHANDLER(int ch, byte cDtmf, int nDTStatus, IntPtr pV);
    public delegate int RECORDMEMBLOCKHANDLER(int ch, int nEndReason, IntPtr pucBuf, uint dwStopOffset, IntPtr pV);
    public delegate int PLAYMEMBLOCKHANDLER(int ch, int nEndReason, IntPtr pucBuf, uint dwStopOffset, IntPtr pV);
    public delegate void LPRECTOMEM(int ch, IntPtr lpData, uint dwDataLen);
    public delegate void LPRECTOMEMB(int ch, IntPtr lpData, uint dwDataLen, IntPtr pV);
    public delegate int LPFNEVENTCALLBACK(ushort wEvent, int nReference, uint dwParam, uint dwUser);
    public delegate int LPFNEVENTCALLBACKA(ref SSM_EVENT pEvent);
    public delegate int LPFNDSTRECRAWDATA(int nCh, uint dwLen, byte[] pucdata, ushort wDataDiscardedTimes, ushort wWriteToFileFailedTimes);

    public class SsmApi {
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@ INITIALIZATION OPERATION  @@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartCti(string lpSsmCfgFileName, string lpIndexCfgFileName);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartCtiEx(string lpSsmCfgFileName, string lpIndexCfgFileName, bool bEnable, ref EVENT_SET_INFO_CALLBACKA pEventSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCloseCti();
        [DllImport("SHP_A3.dll")]
        public static extern void SsmGetLastErrMsg(StringBuilder szErrMsgBuf);
        [DllImport("SHP_A3.dll")]
        public static extern string SsmGetLastErrMsgA();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLastErrCode();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxUsableBoard();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxCfgBoard();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRxDtmfBufSize();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetAccreditId(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxCh();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetChType(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetMaxIdxSeg(ushort wMaxIdxSeg);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmLoadIndexData(int nSegNo, string pAlias, int nCodec, string pVocFile, int lStartPos, int lLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFreeIndexData(int nSegNo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTotalIndexSeg();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPauseCard();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRestartCard();

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetAccreditIdEx(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBoardModel(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBoardName(int nBId, StringBuilder lpBoardModel);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmSetDV(int bEnable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDV();

        [DllImport("SHP_A3.dll")]
        public static extern uint SsmGetPciSerialNo(int nBId);

        [DllImport("SHP_A3.dll")]
        public static extern uint SsmGetIntCount();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetListenMode(int nMode);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartBoard(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopBoard(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetChHdInfo(int ch, ref int vnBId, ref int vnBCh);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetAppChId(ref int AppchId, int BrdId, int BrdChId);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetChState(int ch, int nState);
        [DllImport("SHP_A3.dll")]
        public static extern int StartTimer(int ch, ushort ClockType);
        [DllImport("SHP_A3.dll")]
        public static extern uint ElapseTime(int ch, ushort ClockType);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetLogOutput(int nAPIDbg, int nEventStart, int nEventEnd, int nChStart, int nChEnd, uint dwReserve);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetLogEnable(int nLogType, int nLogEnable, int nLogCreateMode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetLogAttribute(int nLogCreatePeriod, int nLogMaxKeep, int nLogMaxPeriod, string pLogFilePath);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetApiLogRange(int nChStart, int nChEnd, int nEventStart, int nEventEnd);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLogAttribute(ref int pLogCreatePeriod, ref int pLogMaxKeep, ref int pLogMaxPeriod, StringBuilder pLogFilePath);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmLoadChIndexData(int ch, int nSegNo, string pAlias, int nCodec, string pVocFile, long lStartPos, long lLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFreeChIndexData(int ch, int nSegNo);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDllVersion(ref SSM_VERSION vDLLVersion);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ PLAY OPERATION @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPlayVolume(int ch, int nVolume);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetDtmfStopPlay(int ch, int bDspf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDtmfStopPlayFlag(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetBargeinStopPlay(int ch, int bBispf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBargeinStopPlayFlag(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayFile(int ch, string pszFileName, int nFormat, uint dwStartPos, uint dwLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlayFile(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPausePlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRestartPlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFastFwdPlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFastBwdPlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPlayTime(int ch, uint dwTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPlayedTimeEx(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPlayingFileInfo(int ch, ref int vnFormat, ref int vnTotalTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPlayPrct(int ch, uint dwPercentage);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPlayedTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPlayedPercentage(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDataBytesToPlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckPlay(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPlayType(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayIndexString(int ch, string pszIdxStr);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayIndexList(int ch, ushort wIdxListLen, ref ushort vwIdxList);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlayIndex(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearFileList(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmAddToFileList(int ch, string pszFileName, int nFormat, uint dwStartPos, uint dwLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayFileList(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlayFileList(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayMem(int ch, int nFormat, byte[] pBuf, uint dwBufSize, uint dwStartOffset, uint dwStopOffset);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayMem(int ch, int nFormat, IntPtr pBuf, uint dwBufSize, uint dwStartOffset, uint dwStopOffset);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPlayOffset(int ch, ref uint vdwPlayOffset);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetStopPlayOffset(int ch, uint dwStopPlayOffset);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlayMem(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearPlayMemList();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmAddToPlayMemList(byte[] pBuf, uint dwDataLen, int nFormat);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmAddToPlayMemList(IntPtr pBuf, uint dwDataLen, int nFormat);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayMemList(int ch, ushort[] pMemList, ushort wMemListLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayMemList(int ch, IntPtr pMemList, ushort wMemListLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlayMemList(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetDTMFStopPlayCharSet(int ch, string lpstrDtmfCharSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDTMFStopPlayCharSet(int ch, StringBuilder lpstrDtmfCharSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetHangupStopPlayFlag(int ch, int bHangupStopRecFlag);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayMemBlock(int ch, int nFormat, byte[] pBuf, uint dwBufSize, PLAYMEMBLOCKHANDLER OnPlayMemBlockDone, IntPtr pV);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPlayMemBlock(int ch, int nFormat, IntPtr pBuf, uint dwBufSize, PLAYMEMBLOCKHANDLER OnPlayMemBlockDone, IntPtr pV);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopPlayMemBlock(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDataBytesPlayed(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPlayGain(int ch, ushort wGainLevel);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetLine0OutTo(int bEnable);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@    RECORD OPERATION   @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecVolume(int ch, int nVolume);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRecType(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecToFile(int ch, string pszFileName, int nFormat, uint dwStartPos, uint dwBytes, uint dwTime, int nMask);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecToFileA(int ch, string pszFileName, int nFormat,
            uint dwStartPos, uint dwBytes, uint dwTime, int nMask,
            LPRECTOMEM pfnRecToMem);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecToFileB(int ch, string pszFileName, int nFormat,
            uint dwStartPos, uint dwBytes, uint dwTime, int nMask,
            LPRECTOMEMB pfnRecToMem, IntPtr pV);


        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecToFileEx(int ch, string pszFileName, int nFormat,
            uint dwStartPos, uint dwBytes, uint dwTime, int nMask,
            int bSaveToFileOnBargin, uint dwRollbackTime);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmPauseRecToFile(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRestartRecToFile(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRecTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDataBytesToRecord(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRecToFile(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkRecToFile(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecToMem(int ch, int nFormat, byte[] pBuf, uint dwBufSize, uint dwStartOffset);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecToMem(int ch, int nFormat, IntPtr pBuf, uint dwBufSize, uint dwStartOffset);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRecToMem(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRecOffset(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRecAGCSwitch(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecAGC(int ch, int bEnable);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpRecMixer(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecMixer(int ch, int bEnRecMixer, int nMixerVolume);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRecMixerState(int ch, ref int vnEnRecMixer, ref int vnMixerVolume);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPrerecord(int ch, int bEnable, int nMode, ushort wInsertTime, int nFormat);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTruncateTail(int ch, uint dwTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTruncateTailTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPrerecordState(int ch, ref int vnMode, ref ushort vwInsertTime, ref int vnFormat);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetDTMFStopRecCharSet(int ch, string lpstrDtmfCharSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDTMFStopRecCharSet(int ch, StringBuilder lpstrDtmfCharSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetHangupStopRecFlag(int ch, int bHangupStopRecFlag);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckRecord(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecordMemBlock(int ch, int nFormat, byte[] pBuf,
            uint dwBufSize, RECORDMEMBLOCKHANDLER OnRecMemBlockDone, IntPtr pV);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecordMemBlock(int ch, int nFormat, IntPtr pBuf,
            uint dwBufSize, RECORDMEMBLOCKHANDLER OnRecMemBlockDone, IntPtr pV);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRecordMemBlock(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetNoModuleChBusRec(int ch, int bBusRec);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmDstGetWorkMode(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmRecRawData(int ch, LPFNDSTRECRAWDATA DSTRecRawData);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRecRawData(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecRawCtrl0(int ch, ushort wCtrlData);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecRawCtrl1(int ch, ushort wCtrlData);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRecBitFlow(int ch, LPFNDSTRECRAWDATA DSTRecBitFlow);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRecBitFlow(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecordAndPlayUseAsIp(int bEnable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRecordAndPlayUseAsIp();

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@   RxDTMF OPERATION   @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmEnableRxDtmf(int ch, int bRun);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearRxDtmfBuf(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDtmfStr(int ch, StringBuilder pszDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern string SsmGetDtmfStrA(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRxDtmfLen(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmf(int ch, ref char dtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmf(int ch, ref byte dtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmf(int ch, IntPtr dtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmf(int ch, char[] pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmf(int ch, byte[] pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmfClr(int ch, ref char pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmfClr(int ch, ref byte pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmfClr(int ch, IntPtr pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmfClr(int ch, byte[] pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stDtmfClr(int ch, char[] pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLastDtmf(int ch, ref char pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLastDtmf(int ch, ref byte pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLastDtmf(int ch, IntPtr pcDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetWaitDtmf(int ch, ushort wTimeOut, ushort wMaxLen, byte cEndChar, int bWithEndChar);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkWaitDtmf(int ch, StringBuilder pszDtmf);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmCancelWaitDtmf(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetWaitDtmfEx(int ch, ushort wTimeOut, ushort wMaxLen, byte cEndChar, int bWithEndChar);

        // 设置接收DTMF字符回调函数：
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRxDtmfHandler(int ch, RXDTMFHANDLER OnRcvDtmf, IntPtr pV);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetWaitDtmfExA(int ch, ushort wTimeOut, ushort wMaxLen, byte[] szEndChar, int bWithEndChar);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetWaitDtmfExA(int ch, ushort wTimeOut, ushort wMaxLen, string szEndChar, int bWithEndChar);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@ TXDTMF OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryTxDtmf(int ch, string pszDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryTxFlash(int ch, string pszDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTxDtmfPara(int ch, ushort wOnTime, ushort wOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTxDtmfPara(int ch, ref ushort wOnTime, ref ushort wOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmTxDtmf(int ch, string pszDtmf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopTxDtmf(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkTxDtmf(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmTxFlash(int ch, ushort time);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkTxFlash(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTxFlashCharTime(int ch, ushort time);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTxFlashCharTime(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@ INTER-CH OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetLocalFlashTime(int nFlashTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetASDT(int ch, int bEnAutoSendDialTone);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetASDT(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetASTOD(int ch, int bEnAutoSendDialTone);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetASTOD(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetFlashCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearFlashCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetHookState(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRing(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRing(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRingWithFskCID(int ch, byte[] pBuf, uint dwMaxBit, uint dw1stRingOffDelay);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRingWithCIDStr(int ch, string pBuf, uint dwLen, uint dw1stRingOffDelay);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckSendRing(int ch, ref int vnCnt);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@ SEND TONE OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpSendTone(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendTone(int ch, int nToneType);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendToneEx(int ch, uint dwOnTime, uint dwOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopSendTone(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTxTonePara(int ch, int nFreq1, int nVolume1, int nFreq2, int nVolume2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTxTonePara(int ch, ref int vnFreq1, ref int vnVolume1, ref int vnFreq2, ref int vnVolume2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkSendTone(int ch, ref int vnToneType);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@ TONE ANALYZE OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpToneAnalyze(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearToneAnalyzeResult(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartToneAnalyze(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCloseToneAnalyze(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetOverallEnergy(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetVocFxFlag(int ch, int nSelFx, int bClear);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetToneAnalyzeResult(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBusyToneLen(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBusyToneCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRingEchoToneTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBusyToneEx(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTonePara(int ch, ushort wToneFreq1, ushort wToneBW1, ushort wToneFreq2, ushort wToneBW2, uint dwIsToneRatio);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIsDialToneDtrTime(int ch, ushort wIsDialToneDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetBusyTonePeriod(int ch, ushort wBusyTonePeriod);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIsBusyToneDtrCnt(int ch, ushort wIsBusyToneDtrCnt);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRingEchoTonePara(int ch, ushort wRingEchoOnTime, ushort wRingEchoOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetVoiceFxPara(int ch, ushort wSelFx, ushort wFx, ushort wFxBW, uint dwIsVocFxRatio, ushort wIsVocFxDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetVoiceOnDetermineTime(int ch, ushort wIsVocDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetMinVocDtrEnergy(int ch, uint dwMinVocDtrEnergy);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTonePara(int ch, ref ushort vwToneFreq1, ref ushort vwToneBW1, ref ushort vwToneFreq2, ref ushort vwToneBW2, ref uint vdwIsToneRatio);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsDialToneDtrTime(int ch, ref ushort vwIsDialToneDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBusyTonePeriod(int ch, ref ushort vwBusyTonePeriod);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsBusyToneDtrCnt(int ch, ref ushort vwIsBusyToneDtrCnt);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRingEchoTonePara(int ch, ref ushort vwRingEchoOnTime, ref ushort vwRingEchoOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsNoVocDtrmTime(int ch, ref ushort vwIsNoVocDtrmTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetVoiceFxPara(int ch, ushort wSelFx, ref ushort vwFx, ref ushort vwFxBW, ref uint vdwIsVocFxRatio, ref ushort vwIsVocFxDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetVoiceOnDetermineTime(int ch, ref ushort vwIsVocDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMinVocDtrEnergy(int ch, ref uint vdwMinVocDtrEnergy);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpPeakFrqDetect(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPeakFrqDetectBW(int ch, ushort nPeakBW);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPeakFrqDetectBW(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPeakFrqEnergy(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPeakFrq(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint SsmGetRecPlayEnergy(int ch, uint dwMask);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmStart2ndToneAnalyzer(int ch, int bEn);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndToneAnalyzerState(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSet2ndTonePara(int ch, ushort wToneFreq1, ushort wToneBW1, ushort wToneFreq2, ushort wToneBW2, uint dwIsToneRatio);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndTonePara(int ch, ref ushort vwToneFreq1, ref ushort vwToneBW1, ref ushort vwToneFreq2, ref ushort vwToneBW2, ref uint vdwIsToneRatio);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndToneAnalyzeResult(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClear2ndToneAnalyzeResult(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndBusyToneLen(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndBusyToneCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSet2ndBusyTonePeriod(int ch, ushort wBusyTonePeriod);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndBusyTonePeriod(int ch, ref ushort vwBusyTonePeriod);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSet2ndIsBusyToneDtrCnt(int ch, ushort wIsBusyToneDtrCnt);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndIsBusyToneDtrCnt(int ch, ref ushort vwIsBusyToneDtrCnt);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSet2ndIsDialToneDtrTime(int ch, ushort wIsDialToneDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndIsDialToneDtrTime(int ch, ref ushort vwIsDialToneDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSet2ndRingEchoTonePara(int ch, ushort wRingEchoOnTime, ushort wRingEchoOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet2ndRingEchoTonePara(int ch, ref ushort vwRingEchoOnTime, ref ushort vwRingEchoOffTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetBusyTonePeriodEx(int ch, int nType, ushort wMax, ref ushort vwPeriod);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBusyTonePeriodEx(int ch, int nType, ref ushort vwPeriod);
        [DllImport("SHP_A3.dll")]
        public static extern ushort SsmGetToneValue(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetToneValue(int ch, ushort value);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetOverallEnergyAllCh(int nBeginCh, int nChNum, ref uint vdwEnergyTable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDstChSNRofUplink(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDstChSNRofDownlink(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@ BARGEIN OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetNoSoundDtrmTime(int ch, uint dwIsNoSoundDtrTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIsBargeInDtrmTime(int ch, ushort wIsBargeInDtrmTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetBargeInSens(int ch, int nBargeInSens);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetNoSoundTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetNoSoundDtrmTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsBargeInDtrmTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBargeInSens(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmDetectBargeIn(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmDetectNoSound(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetVoiceEnergyMinValue(int ch, uint nVoiceEnergyMinValue);
        [DllImport("SHP_A3.dll")]
        public static extern uint SsmGetVoiceEnergyMinValue(int ch);

        //设置模拟电话线相对能量被叫摘机检测参数
        //返回值 0:操作成功, -1:操作失败
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetCalleeHookDetectP(int ch, // 通道号
            ushort wMulti, //预定倍数
            ushort wValidTime); //有效时间
        //读取模拟电话线相对能量被叫摘机检测参数
        //返回值 0:操作成功, -1:操作失败
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCalleeHookDetectP(int ch, // 通道号
            ref ushort vwMulti, //预定倍数
            ref ushort vwValidTime); //有效时间


        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@ RING DETECT OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpRingDetect(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRingFlag(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRingCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearRingCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkOpCallerId(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCallerId(int ch, byte[] szCallerId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCallerId(int ch, StringBuilder szCallerId);
        [DllImport("SHP_A3.dll")]
        public static extern string SsmGetCallerIdA(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCallerIdEx(int ch, byte[] szCallerIdEx);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCallerIdEx(int ch, StringBuilder szCallerIdEx);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearCallerId(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearCallerIdEx(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCallerName(int ch, byte[] szCallerName);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCallerName(int ch, StringBuilder szCallerName);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@ CALL OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPickup(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckActualPickup(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPickupNow(int ch, int bFlag);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmHangup(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmHangupEx(int ch, byte ucCauseVal);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmAutoDial(int ch, string szPhoNum);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChkAutoDial(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetAutoDialFailureReason(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetBlockReason(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetChState(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPlayDest(int ch, int nSelDest);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRecBack(int ch, int nRecBack);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSearchIdleCallOutCh(ushort wSearchMode, uint dwPrecedence);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetAutoCallDirection(int ch, int bEnAutoCall, int nDirection);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetAutoCallDirection(int ch, ref int vnDirection);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmAppendPhoNum(int ch, string szPhoNum);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPendingReason(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetChStateKeepTime(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPhoNumStr(int ch, StringBuilder pszPhoNum);
        [DllImport("SHP_A3.dll")]
        public static extern string SsmGetPhoNumStrA(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPhoNumLen(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stPhoNumStr(int ch, StringBuilder pszPhoNum);
        [DllImport("SHP_A3.dll")]
        public static extern string SsmGet1stPhoNumStrA(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGet1stPhoNumLen(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmEnableAutoSendKB(int ch, int bEnable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetAutoSendKBFlag(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetKB(int ch, byte btSigKB);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetKD(int ch, byte btSigKD);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetKA(int ch, byte btSigKA);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTxCallerId(int ch, string pszTxCallerId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTxCallerId(int ch, StringBuilder pszTxCallerId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetKA(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetKB(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetKD(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmAutoDialEx(int ch, string szPhoNum, ushort wParam);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmIsHaveCpg(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCpg(int ch, StringBuilder szMsg, ref int msglen);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTxOriginalCallerID(int ch, StringBuilder pszTxCallerId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTxRedirectingNum(int ch, StringBuilder pszTxRedirectingNum);

        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //	Following functions for applications using SS7
        //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        [DllImport("SHP_A3.dll")]
        public static extern int SsmBlockLocalCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnblockLocalCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryLocalChBlockState(int ch, ref uint vdwBlockState);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmBlockLocalPCM(int nLocalPcmNo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnblockLocalPCM(int nLocalPcmNo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryLocalPCMBlockState(int nLocalPcmNo, ref uint vdwBlockState);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpBlockRemoteCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmBlockRemoteCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnblockRemoteCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRemoteChBlockStatus(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmBlockRemotePCM(int nLocalPcmNo, uint dwBlockMode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnblockRemotePCM(int nLocalPcmNo, uint dwUnblockMode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRemotePCMBlockStatus(int nLocalPcmNo, uint dwBlockMode);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetWaitAutoDialAnswerTime(ref ushort vwSeconds);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetWaitAutoDialAnswerTime(ushort wSeconds);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartPickupAnalyze(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPickup(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern ushort SsmGetReleaseReason(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@ ADAPTIVE FILTER OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpEchoCanceller(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetEchoCanceller(int ch, int bRun);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetEchoCancellerState(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetEchoCancellerStudy(int ch, int bRun);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetEchoCancellerStudyState(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetEchoCancellerRatio(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSaveEchoCancellerPara(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetEchoCancelDelaySize(int ch, ushort wSize);
        [DllImport("SHP_A3.dll")]
        public static extern ushort SsmGetEchoCancelDelaySize(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@ CT-BUS OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmListenTo(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmListenToEx(int ch1, int nVolume1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopListenTo(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmTalkWith(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmTalkWithEx(int ch1, int nVlm1, int ch2, int nVlm2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopTalkWith(int ch1, int ch2);

        [DllImport("SHP_A3.dll")]
        public static extern void PlayListen(uint dwBId, uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void PlayListenNew(uint dwSpeakerCh, uint dwMonitorCh);
        [DllImport("SHP_A3.dll")]
        public static extern void StopListen(uint dwBId);
        [DllImport("SHP_A3.dll")]
        public static extern void StopListenNew(uint dwSpeakerCh);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmLinkFrom(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopLinkFrom(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmLinkFromEx(int ch1, int nVolume1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmLinkFromAllCh(int ch, int nVolume, ref int nListenerTable, int nListenerNum);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnLinkFromAllCh(int ch, ref int nListenerTable, int nListenerNum);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmLinkToBus(int ch, int ts);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnLinkToBus(int ch, int ts);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmLinkFromBus(int ts, int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmLinkFromBusEx(int ts, int ch, int vlm);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnLinkFromBus(int ts, int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmListenToPlay(int ch1, int vlm1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnListenToPlay(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearChBusLink(int nCh);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ CONFERENCE OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCreateConfGroup(int nMaxMember, int nMaxSpeaker, int nMaxSpeaking, int nMaxSilenceTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFreeConfGroup(int nGrpId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmJoinConfGroup(int nGrpId, int ch, ushort wJoinMode, int nMixerVolume, int bCreateAlways, int bExitGrpAlways);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmExitConfGroup(int ch, int bFreeGrpAlways);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfCfgInfo(ref ushort vwMaxMember, ref ushort vwMaxSpeaker, ref ushort vwMaxSpeaking, ref ushort vwMaxSilenceTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTotalConfGroup();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfGrpId(ref int vnGrpId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfGrpCfgInfo(int nGrpId, ref ushort vwMaxMember, ref ushort vwMaxSpeaker, ref ushort vwMaxSpeaking, ref ushort vwMaxSilenceTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfGrpInfo(int nGrpId, ref ushort vwTotalMember, ref ushort vwTotalSpeaker, ref ushort vwTotalSpeaking);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfGrpMmbrId(int nGrpId, ref int vnMmbrId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfGrpMmbrInfo(int nGrpId, int nMmbrId, ref int vnAppCh, ref ushort vwJoinMode, ref ushort vwIsSpeaking, ref uint vdwSilenceTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetConfChInfo(int ch, ref int vnGrpId, ref int vnMmbrId, ref ushort vwJoinMode, ref ushort vwIsSpeaking, ref uint vdwSilenceTime);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmValidateGrpId(int nGrpId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetListenVlmInConf(int ch, int nVlm);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@ DRIVER  Ver. 1.x COMPATIBLE FUNCTIONS  @@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern uint InitCard(uint add1, uint add2, uint add3, uint add4, uint add5,
            uint add6, uint add7, uint add8, uint intno);
        [DllImport("SHP_A3.dll")]
        public static extern void ShutCard();

        [DllImport("SHP_A3.dll")]
        public static extern uint StartRecordFile(uint ch, string filename);
        [DllImport("SHP_A3.dll")]
        public static extern void StopRecordFile(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void PauseRecord(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void RestartRecord(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetRecordTime(uint ch);

        [DllImport("SHP_A3.dll")]
        public static extern uint StartPlayFile(uint ch, string filename);
        [DllImport("SHP_A3.dll")]
        public static extern void StopPlayFile(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint CheckPlayingEnd(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void PausePlay(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void RestartPlay(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void FastPlay(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void BackPlay(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetPlayTime(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetPlayPercent(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SetPlayTime(uint ch, uint time);
        [DllImport("SHP_A3.dll")]
        public static extern void SetPlayPercent(uint ch, uint percent);

        [DllImport("SHP_A3.dll")]
        public static extern uint LoadIndexFile(uint segno, string filename, uint startadr, uint length);
        [DllImport("SHP_A3.dll")]
        public static extern void FreeIndexMem(uint segno);
        [DllImport("SHP_A3.dll")]
        public static extern void PlayIndex(uint ch, string segstring);
        [DllImport("SHP_A3.dll")]
        public static extern void StopIndex(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void DTMFStop(uint ch, uint if_stop);

        [DllImport("SHP_A3.dll")]
        public static extern uint GetDTMF(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetDTMFStr(uint ch, StringBuilder buf);
        [DllImport("SHP_A3.dll")]
        public static extern void ClearDTMFBuf(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint DetectRing(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void ClearRing(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void HangUp(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void PickUp(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint DetectInter(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetFlash(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SetFlashTime(int time);
        [DllImport("SHP_A3.dll")]
        public static extern uint ToneCheck(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetBusyLen(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void DTMFDial(uint ch, string dialstring);
        [DllImport("SHP_A3.dll")]
        public static extern uint DTMFDialEnd(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void Flash(uint ch, uint time);
        [DllImport("SHP_A3.dll")]
        public static extern void SetPlayVolume(uint ch, int volume);
        [DllImport("SHP_A3.dll")]
        public static extern void SetRecVolume(uint ch, int volume);

        [DllImport("SHP_A3.dll")]
        public static extern uint GetCallerId(uint ch, StringBuilder buf);

        [DllImport("SHP_A3.dll")]
        public static extern void SetPickSens(uint piont);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetPickUp(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetNum(uint ch, StringBuilder buf, uint time, uint len, int end_char);
        [DllImport("SHP_A3.dll")]
        public static extern void SetWorkMode(int Mode);
        [DllImport("SHP_A3.dll")]
        public static extern void SetUserCard(uint addr1, uint addr2, uint addr3, uint addr4, uint addr5, uint addr6, uint addr7, uint addr8);
        [DllImport("SHP_A3.dll")]
        public static extern void SendBusyTone(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SendRingEchoTone(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SendDialTone(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void StopSendTone(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void StartSendRing(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void StopSendRing(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int Link2Ch(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern void UnLink2Ch(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern int Link3Ch(int ch1, int ch2, int ch3);
        [DllImport("SHP_A3.dll")]
        public static extern void UnLink3Ch(int ch1, int ch2, int ch3);
        [DllImport("SHP_A3.dll")]
        public static extern int ListenFromCh(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern void StopListenFromCh(int ch1, int ch2);
        [DllImport("SHP_A3.dll")]
        public static extern void SetDelay(uint ch, uint delay);
        [DllImport("SHP_A3.dll")]
        public static extern void SetLevel(uint ch, uint level);
        [DllImport("SHP_A3.dll")]
        public static extern uint DetectSound(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetFax11(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetFax21(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetRing(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SetDialSpeed(uint point);
        [DllImport("SHP_A3.dll")]
        public static extern void GetErrorMsg(StringBuilder buf);
        [DllImport("SHP_A3.dll")]
        public static extern uint SetMaxSeg(uint inmaxsegment);

        [DllImport("SHP_A3.dll")]
        public static extern void PauseCard();
        [DllImport("SHP_A3.dll")]
        public static extern void RestartCard();

        [DllImport("SHP_A3.dll")]
        public static extern int SetIRQPriority(int nPriorityClass);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ RECORD MODULE OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern uint GetLevel(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SetJudge(uint ch, uint Judge);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpADC(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLineVoltage(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetDtrmLineVoltage(int ch, ushort wDtrmValtage);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetDtrmLineVoltage(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint DetectPickUp(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern uint DetectEmpty(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SetSoundJudgeLevel(uint ch, uint level);
        [DllImport("SHP_A3.dll")]
        public static extern uint GetSoundJudgeLevel(uint ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpMicGain(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetMicGain(int ch, int nGain);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMicGain(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIgnoreLineVoltage(int ch, int bEnable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIgnoreLineVoltage(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ POWER-AMPLIFIER OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern void SetVolume(uint dwBoardId, uint dwVolume);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpPowerAmp(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPowerAmpVlm(int ch, int nVolume);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ POLAR REVERSE OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern uint GetFZCount(uint ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpPolarRvrs(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPolarRvrsCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPolarState(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPolarState(int ch, int nPolar);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetUnimoduleState(int ch, int nLink);
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ SS1 OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryOpSS1(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendCAS(int ch, byte btCas);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCAS(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetRxR2Mode(int ch, int nMode, int bEnable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetR2(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendR2(int ch, int nMode, byte btR2);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopSendR2(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetSendingCAS(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetSendingR2(int ch, ref int vnMode, ref byte pbtR2);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ PCM LINK OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxPcm();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPcmInfo(int nPcmNo, ref int vnSSxMode, ref int vnBoardId, ref int vnBoardPcmNo, ref int vnUsePcmTS16,
            ref uint vdwRcvrMode, ref uint vdwEnableAutoCall, ref uint vdwAutoCallDirection);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetPcmClockMode(int nPcmNo, int nClockMode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetPcmLinkStatus(int nPcmNo, ref ushort vwPcmLinkStatus);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPcmTsToCh(int nLocalPcmNo, int nTs);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmChToPcmTs(int ch, ref int vnLocalPcmNo, ref int vnTs);


        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ SEND FSK OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetFskPara(int nFreqBit0, int nFreqBit1, int nBaudrate, int nMdlAmp);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetFskPara(ref int vnFreqBit0, ref int vnFreqBit1, ref int vnBaudrate, ref int vnMdlAmp);
        //	[DllImport("SHP_A3.dll")]public static extern int SsmTransFskData(char[]  pS, int	nSrcLen,int nSyncLen,int nSyncOffLen,char[]  pD);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmTransFskData(byte[] pS, int nSrcLen, int nSyncLen, int nSyncOffLen, byte[] pD);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartSendFSK(int ch, string pBuf, uint dwMaxBit);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckSendFsk(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopSendFsk(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ RECEIVE FSK OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //[DllImport("SHP_A3.dll")]public static extern int SsmStartRcvFSK_III(int ch, ushort wTimeOut, ushort wMaxLen, char[] pucMarkCodeBuf, char ucMarkCodeCount);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRcvFSK_III(int ch, ushort wTimeOut, ushort wMaxLen, byte[] pucMarkCodeBuf, byte ucMarkCodeCount);
        //[DllImport("SHP_A3.dll")]public static extern int SsmStartRcvFSK_II(int ch, ushort wTimeOut, ushort wMaxLen, char[] pucMarkCodeBuf, char ucMarkCodeCount);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRcvFSK_II(int ch, ushort wTimeOut, ushort wMaxLen, byte[] pucMarkCodeBuf, byte ucMarkCodeCount);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRcvFSK(int ch, ushort wTimeOut, ushort wMaxLen, byte ucEndCode, ushort wEndCodeCount);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmClearRcvFSKBuf(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckRcvFSK(int ch);
        //[DllImport("SHP_A3.dll")]public static extern int SsmGetRcvFSK(int ch, char[] pucBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRcvFSK(int ch, byte[] pucBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopRcvFSK(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ SS7 OPERATION  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendSs7Msu(ushort wMsuLength, byte[] pucMsuBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetSs7Msu(ref byte[] ppucMsuBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetSs7Msu(ref IntPtr ppucMsuBuf);
        //[DllImport("SHP_A3.dll")]public static extern int SsmGetSs7Mtp2Msu(ref char pucPara, char[][] ppucMsuBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMtp3State();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMtp3StateEx(int nDpcNo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMtp2Status(int nLinkNum);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetISUPCAT(int nch, byte ucCallerCAT);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsupUPPara(int nBCh, ushort wEventType, ref ushort vwLength, byte[] pucContent);
        //	[DllImport("SHP_A3.dll")]public static extern int SsmGetIsupUPPara(int nBCh, ushort wEventType, ref ushort vwLength, char[] pucContent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIsupUPPara(int nBCh, ushort wEventType, ref ushort vwLength, byte[] pucContent);
        //  [DllImport("SHP_A3.dll")]public static extern int SsmSetIsupUPPara(int nBCh, ushort wEventType, ref ushort vwLength, char[] pucContent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendIsupMsg(int nBCh, ushort wEventType);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetInboundLinkSet(int nBCh, ref ushort vwLinkSetNo, StringBuilder pszOpc, StringBuilder pszDpc);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRedirectionInfReason(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIsupFlag(int ch, int nType, uint dwValue, IntPtr pV);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsupFlag(int ch, int nType, ref uint pd);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIsupParameter(int nBCh, byte ucMsgTypeCode, byte ucParamTypeCode, ushort wLength, ref byte pucContent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsupParameter(int nBCh, byte ucMsgTypeCode, byte ucParamTypeCode, ref byte pucContent, ushort wNumberOfBytesToWrite, ref ushort lpNumberOfBytesWritten);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetTupParameter(int nBCh, byte ucMsgTypeCode, byte ucParamTypeCode, ushort wLength, ref byte pucContent);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmIsupGetUsr(ref int ch, byte[] pucData, ref byte ucLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIsupSendUsr(int ch, byte[] pucData, byte ucLen);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendSs7MsuEx(int ch, int nNewStep, ushort wMsuLength, byte[] pucMsuBuf);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendSs7Mtp2Msu(int ss7link, ushort wMsuLength, byte[] pucMsuBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxSs7link();

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@ FAX OPERATION API @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetChStateMsg(int ch, StringBuilder buf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetPages(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxStartReceive(int ch, string filename);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxStartSend(int ch, string filename);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxStop(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmFaxSetMaxSpeed(int speed);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxCheckEnd(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSetID(int ch, string myid);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetID(int ch, StringBuilder myid);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSetSendMode(int ch, int mode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetRcvBytes(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetSendBytes(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxAppendSend(int ch, string filename);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSetHangup(int ch, int flag);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxStartSendEx(int ch, string filename, int nStartPage, int nEndPage);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSendMultiFile(int ch, string szFilePath, string szFileName);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSendMultiFileEx(int ch, IntPtr pV, int nNum);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSendMultiFileEx(int ch, FAX_FILE_SCT[] pV, int nNum);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetAllBytes(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetSpeed(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetMode(int ch, ref int vnDir, ref int vnResMode, ref int vnTransMode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxSetTransMode(int ch, int nMode);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetFailReason(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetDcnTag(int ch);
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ MODEM OPERATION API  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmEnableCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmDisableCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmResetCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckResetCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetLSR(int ch, ref byte retu);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMSR(int ch, ref byte retu);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRTS(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCTS(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetOH(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmDetectCarry(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmFaxGetChState(int ch, ref ushort buf);


        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ SERIAL PORT IO OPERATION API @@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmReadRxBuf(int ch, int nLen, StringBuilder lpcRxBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetRxBufLen(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmClearRxBuf(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmWriteTxBuf(int ch, int nLen, StringBuilder lpcTxBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmWriteTxBuf_S(int ch, StringBuilder s);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmWriteTxBuf_C(int ch, byte buf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTxBufRemain(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetTxBufLen(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmClearTxBuf(int ch);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ ISDN API @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNSetDialSubAddr(int ch, string lpSubAddress);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNSetTxSubAddr(int ch, string lpSubAddress);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNGetTxCallerSubAddr(int ch, StringBuilder lpSubAddress);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNGetSubAddr(int ch, StringBuilder lpSubAddress);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNGetCallerSubAddr(int ch, StringBuilder lpSubAddress);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNGetDisplayMsg(int ch, StringBuilder lpDispMsg);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNSetHangupRzn(int ch, int nReason);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNGetStatus(int nPcmNo, ref int pL3Start,
            ref int pL2DStatus, ref int pL2D_L3Atom,
            ref int pL3_L2DAtom, ref int pRef_ind);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetUserCallerId(int ch, StringBuilder szCallerId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmISDNSetCallerIdPresent(int ch, byte ucPresentation);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetNumType(int ch, int nNumClass, int nNumType);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetNumType(int ch, int nNumClass, ref int pNumType);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetCharge(int ch, int ChargeFlag);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetUserInfo(int ch, byte[] pUUI);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ ViaVoice recognize OPERATION API @@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartRecognize(int ch, int grammarid, int max_time);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetResultCount(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetCurSens(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetResult(StringBuilder buf, StringBuilder index, StringBuilder score, int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetNResult(int id, StringBuilder buf, StringBuilder index, StringBuilder score, int ch);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmSetSil(int nValue);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmSetRecoSound(int nBeginLen, int nEndLen, int nSoundLen);
        [DllImport("SHP_A3.dll")]
        public static extern void SsmSetRecoTime(int nRecoTime, int nMaxWait);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ Set Hardware Flags OPERATION API @@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetFlag(int ch, int nType, int lValue);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetFlag(int ch, int nType, ref int plValue);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmDstSetFlag(int ch, int nType, long lValue, IntPtr pV);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetOvrlEnrgLevel(int ch, ushort wOvrlEnrgLevel);
        [DllImport("SHP_A3.dll")]
        public static extern ushort SsmGetOvrlEnrgLevel(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetOvrlEnrgDtrmTime(int ch, ushort wMinTime, ushort wMaxTime);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ SPY  API @@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetState(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern uint SpyGetHangupInfo(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetCallerId(int nCic, StringBuilder pcCid);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetCalleeId(int nCic, StringBuilder pcCid);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetCallerType(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetCalleeType(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetConId(int nCic, StringBuilder pcCid);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetCallInCh(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetCallOutCh(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetMaxCic();
        [DllImport("SHP_A3.dll")]
        public static extern int SpyStopRecToFile(int nCic);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyRecToFile(int nCic, ushort wDirection,
            string pszFileName, int nCodecFormat,
            uint dwStartPos, uint dwBytes,
            uint dwTime, int nMask);
        [DllImport("SHP_A3.dll")]
        public static extern int SpyGetLinkStatus(int nSpyPcmNo, byte ucFlag);

        [DllImport("SHP_A3.dll")]
        public static extern int SpyChToCic(int ch);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetSs7SpyMsu(byte[][] ppucMsuBuf);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ EVENT  API @@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetEvent(ushort wEvent, int nReference, int bEnable, ref EVENT_SET_INFO pEventSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetEvent(ushort wEvent, int nReference, int bEnable, ref EVENT_SET_INFO_CALLBACK pEventSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetEvent(ushort wEvent, int nReference, int bEnable, ref EVENT_SET_INFO_CALLBACKA pEventSet);

        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetEventMode(ushort wEvent, int nReference, ref ushort vwEnable, ref EVENT_SET_INFO pEventSet);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmWaitForEvent(uint dwTimeOut, ref MESSAGE_INFO pEvent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmWaitForEventA(uint dwTimeOut, ref SSM_EVENT pEvent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetEvent(ref MESSAGE_INFO pEvent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetEventA(ref SSM_EVENT pEvent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartTimer(ushort wDelay, ushort fuEvent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStopTimer(int nTimer);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPutUserEvent(ushort wEventCode, int nReference, uint dwParam);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmPutUserEventA(ref SSM_EVENT pEvent);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetInterEventType();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetInterEventType(int nType);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@   DTR OPERATION   @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int DTRGetLCDStr(int ch, StringBuilder pszLCDStr);
        [DllImport("SHP_A3.dll")]
        public static extern int DTRSetMixerVolume(int ch, int nGroup, int nDownVolume, int nUpVolume);
        [DllImport("SHP_A3.dll")]
        public static extern int DTRGetMixerVolume(int ch, int nGroup, ref int vnDownVolume, ref int vnUpVolume);
        [DllImport("SHP_A3.dll")]
        public static extern int DTRGetDKeyStr(int ch, StringBuilder pszDKeyStr);
        [DllImport("SHP_A3.dll")]
        public static extern int DTRSendRawData2A3(int ch, byte ucCmd, ushort wLen, byte[] pucData);
        [DllImport("SHP_A3.dll")]
        public static extern string DTRGetLCDStrA(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetIsdnMsu(int nPcmId, byte[] pucMsuBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSendIsdnMsu(int nPcmId, int nMsgLen, byte[] pucMsuBuf);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCheckIsdnMsu(int nPcmId);


        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@ Tcap and Sccp API  @@@@@@@@@@@@@@@@@@@@@@@@@@@ 
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SccpSaveReceivedMessage(int nLen, byte[] pucMsg);
        [DllImport("SHP_A3.dll")]
        public static extern int SccpGetReceivedMessage(ref int vnLen, byte[] pucMsg);
        [DllImport("SHP_A3.dll")]
        public static extern int MtpGetSccpMessage(ref int vnLen, byte[] pucMsg);
        [DllImport("SHP_A3.dll")]
        public static extern int SccpInit();
        [DllImport("SHP_A3.dll")]
        public static extern int SccpConfig();

        [DllImport("SHP_A3.dll")]
        public static extern int SsmUserSendMessageToTcap(int nLen, byte[] pucMsg);//发消息 消息长度， 消息体
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUserGetTcapDlgMessage(ref int pLen, byte[] pucDlgInd);//长度对话消息体
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUserGetTcapCmpMessage(ref int pLen, int nCurDlgID, int nMsgStyle, byte[] pucCmpInd);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUserGetLocalMessage(ref int pLen, byte[] pucMsgInd);//获得L_Cancel 和 L_Reject
        [DllImport("SHP_A3.dll")]
        public static extern byte[] SsmUserGetISMState(int nID, byte[] pucState);//调用ID状态
        [DllImport("SHP_A3.dll")]
        public static extern int SsmStartTcap();
        [DllImport("SHP_A3.dll")]
        public static extern void SsmTcapGetErrorMsg(byte[][] temp);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@   Query Format   查询通道是否支持指定的录放音格式@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryPlayFormat(int ch, int nFormat);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmQueryRecFormat(int ch, int nFormat);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@   Board Eeprom OPERATION   @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmReadBoardEepromShareSection(int nBId, IntPtr pV, int nLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmWriteBoardEepromShareSection(int nBId, IntPtr pV, int nLen);

        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ 变声资源新增API @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //	低级函数，用于与第三方板卡配合使用
        [DllImport("SHP_A3.dll")]
        public static extern int ShvGetLinkToBus(int iVCh);
        [DllImport("SHP_A3.dll")]
        public static extern int ShvLinkToBus(int iVCh, int iTs);
        [DllImport("SHP_A3.dll")]
        public static extern int ShvUnLinkToBus(int iVCh, int iTs);
        [DllImport("SHP_A3.dll")]
        public static extern int ShvLinkFromBus(int iVCh, int iTs);
        [DllImport("SHP_A3.dll")]
        public static extern int ShvUnLinkFromBus(int iVCh, int iTs);
        [DllImport("SHP_A3.dll")]
        public static extern int ShvSetVoiceEffect(int iVCh, int iValue);
        [DllImport("SHP_A3.dll")]
        public static extern int ShvGetVoiceEffect(int iVCh);
        //	高级函数，与其他语音卡配合使用
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxVCh();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetMaxFreeVCh();
        [DllImport("SHP_A3.dll")]
        public static extern int SsmBindVCh(int iCh);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnBindVCh(int iCh);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetVoiceEffect(int iCh, int iValue);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetVoiceEffect(int iCh);
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@   VOIP Board Operation     @@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIpGetSessionCodecType(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIpSetForwardNum(int ch, string pszForwardNum);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIpInitiateTransfer(int ch, string pszTransferTo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIpGetMessageField(int ch, int type, StringBuilder szBuffer, ref int pSize);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipBoardRegister(int nBId, string szRegSrvAddr, string szUserName, string szPasswd, string szRealm, int nExpires);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipChRegister(int nCh, string szRegSrvAddr, string szUserName, string szPasswd, string szRealm, int nExpires);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipMultiChRegister(int nChFrom, int nChTo, string szRegSrvAddr, string szUserName, string szPasswd, string szRealm, int nExpires);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIpUpdateSystem(string filePath);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipGetReferStatus(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipSetTxUserName(int ch, string pszUserName);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetIpFlag(int ch, int Type, string pszBuffer);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipGetBoardRegStatus(int nBId, StringBuilder pszRegFailInfo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSipGetChRegStatus(int nChId, StringBuilder pszRegFailInfo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmLockMediaCh(int ch);
        //[DllImport("SHP_A3.dll")]
        //public static extern int SsmGetMediaChParam(int ch, ref MediaParam mParam);
        //[DllImport("SHP_A3.dll")]
        //public static extern int SsmOpenMediaCh(int ch, ref MediaParam mParam);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmCloseMediaCh(int ch);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmUnlockMediaCh(int ch);
        //[DllImport("SHP_A3.dll")]
        //public static extern int SsmUpdateMediaCh(int ch, ref MediaParam mParam);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmGetChBusInfo(int ch, ref tagBUS_OP p);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmSetWaitAutoDialAnswerTimeEx(int ch, int nSeconds);

        //+++start+++ added by netwolf for SynIPR,2011.05.23
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@ SynIPR API @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRStartRecSlaver(int nBId, int nRecSlaverId, ref int nTotalResources, ref int nThreadPairs);
        //+++ end +++ added by netwolf for SynIPR,2011.05.23
        //+++start+++	added by wangfeng for synIPR, 2011.05.31
        //IPRecorder Analyzer API:
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRAddProtocol(int nBId, int nPtlId, ref IPR_MONITOR_CFGS pParams, uint dwLen);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRRmvProtocol(int nBId, int nPtlId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetStationCount(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetStationList(int nBId, int nStationNum, IntPtr pStationList);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetStationInfo(int nBId, int nStationId, ref int nPtlId, ref int nTransType, ref int nPort, byte[] szIP, byte[] szMAC);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRSendSession(int nChId, string szPriSlaverAddr, int nPriSlaverPort, string szSecSlaverAddr, int nSecSlaverPort);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRStopSendSession(int nChId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetSessionInfo(int nChId, ref IPR_SessionInfo pIPR_SessionInfo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetSessionID(int nChId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetPTWithSessionID(int nSession, ref int pPT, ref int sPT);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRSetMonitorType(int nBId, int nMType);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetMonitorType(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRAddStationToMap(int nBId, int nStationId, string szAddr, int nPort);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRAddStationToMapEx(int nBId, int nStationId, string szAddr, int nPort, string szUserName, IntPtr pReserve);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRRmvStationFromMap(int nBId, string szAddr, int nPort);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRRmvStationFromMapEx(int nBId, int nStationId, bool bDelAtOnce);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRChkFoward(int nChId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetProtocol(int nPtlId, ref IPR_MONITOR_CFGS pParams, ref uint pdwLen);
        //IPRecorder Recorder Master API:
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRSetRecVolume(int nChId, int nPrimaryVlm, int nSecondaryVlm);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRSetInBandDTMFChkFlag(int nChId, int bEnable);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetInBandDTMFChkFlag(int nChId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRSetMixerType(int nChId, int nFlag);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetMixerType(int nChId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRActiveSession(int nCh, int nRecSlaverId, uint dwSessionId,
           StringBuilder szPriAddr, int nPriPort, ref int pnPriRcvPort, int nPriCodec,
           StringBuilder szSecAddr, int nSecPort, ref int pnSecRcvPort, int nSecCodec);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRDeActiveSession(int nCh);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRActiveAndRecToFile(int nCh, int nSlaverId, uint dwSessionId,
           int nCodec, ref int pnPriRcvPort, ref int pnSecRcvPort,
           string pszFileName, int nFormat, uint dwStartPos, uint dwBytes,
           uint dwTime, int nMask);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRDeActiveAndStopRecToFile(int nCh);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetRecSlaverCount(int nBId);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetRecSlaverList(int nBId, int nRecSlaverNum, ref int nReturnRecSlaverNum, ref IPR_SLAVERADDR pIPR_SlaverAddr);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRGetRecSlaverInfo(int nBId, int nRecSlaverId, ref int bStarted, ref int nTotalResources, ref int nUsedResources, ref int nThreadPairs, ref COMPUTER_INFO pcomputerinfo);
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIPRCloseRecSlaver(int nBId, int nRecSlaverId);
        //+++ end +++	added by wangfeng for synIPR, 2011.05.31
        [DllImport("SHP_A3.dll")]
        public static extern uint SsmGetUSBKeySerial(int nBId);   //add by cyq for iprr and ipra
        [DllImport("SHP_A3.dll")]
        public static extern int SsmIsBoardIPR(int nBID);
    }
}
