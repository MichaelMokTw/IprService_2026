using Dapper;
using MyProject.lib;
using MyProject.Models;
using MyProject.ProjectCtrl;

namespace MyProject.Database {

    public interface IRecDb {
        Task<ExceptionHandle<List<string>?>> GetTableSchema();
        Task<ExceptionHandle<LoggerDataModel?>> GetLogger(string loggerID);
        Task<ExceptionHandle<LoggerSystemConfigDataModel?>> GetLoggerSystemConfig(long loggerSeq);
        Task<ExceptionHandle<List<ConfigBoardDataModel>?>> GetHwBoard(long loggerSeq, char boardType);
        Task<ExceptionHandle<List<ChannelConfigDataModel>?>> GetChannelConfigList(ENUM_ChannelType chType);
        Task<ExceptionHandle<ulong?>> GetMaxRecID(int chType);
        Task<ExceptionHandle<uint>> GetRecFileCount(DateTime recDate, int chType);
        Task<ExceptionHandle<ulong?>> InsertRecData(RecordingDataModel model, int backupFlag);        
        Task<ExceptionHandle<int>> UpdateModuleStatus(string moduleID, int status, string moduleVer, long loggerSeq);
        Task<ExceptionHandle<int>> UpdateRecDataFlags(long recSeq, int backupFlag, int originCodec, int codecFlag, int encryptFlag);
        Task<ExceptionHandle<int>> UpdateRecGuid(long loggerSeq, string extNo, string recGuid);
    }

    class RecDb : _BaseDb, IRecDb {
        private HttpContext? _httpContext = null;

        public async Task<ExceptionHandle<List<string>?>> GetTableSchema() {

            var sql = $@"SELECT TABLE_NAME FROM information_schema.tables WHERE TABLE_TYPE = 'BASE TABLE'";

            return await QueryDBAsync<List<string>?>(
                _httpContext,
                sql,
                null,
                async (conn, query, parameters) => {
                    var result = (await conn.QueryAsync<string>(query, parameters, commandTimeout: DBQueryTimeout)).ToList();
                    return result;
                }
            );

        }

        public async Task<ExceptionHandle<LoggerDataModel?>> GetLogger(string loggerID) {

            var sql = $@"Select * From tblLogger Where LoggerID=@LoggerID";

            return await QueryDBAsync<LoggerDataModel?>(
                _httpContext,
                sql,
                new {
                    @LoggerID = loggerID,
                },
                async (conn, query, parameters) => {
                    var result = (await conn.QueryAsync<LoggerDataModel>(query, parameters, commandTimeout: DBQueryTimeout)).FirstOrDefault();
                    return result;
                }
            );
        }

        public async Task<ExceptionHandle<LoggerSystemConfigDataModel?>> GetLoggerSystemConfig(long loggerSeq) {

            var sql = $@"Select * From tblSystemConfig Where LoggerSeq = @LoggerSeq;";

            return await QueryDBAsync<LoggerSystemConfigDataModel?>(
                _httpContext,
                sql,
                new {
                    @LoggerSeq = loggerSeq
                },
                async (conn, query, parameters) => {
                    var result = (await conn.QueryAsync<LoggerSystemConfigDataModel>(query, parameters, commandTimeout: DBQueryTimeout)).FirstOrDefault();
                    return result;
                }
            );
        }

        public async Task<ExceptionHandle<List<ConfigBoardDataModel>?>> GetHwBoard(long loggerSeq, char boardType) {

            var sql = $@"Select * 
                        From tblHwBoard 
                        Where Status=1 
                                And BoardType=@BoardType 
                                And LoggerSeq=@LoggerSeq 
                        Order By BoardType, LogicBeginChNo ASC;";

            return await QueryDBAsync<List<ConfigBoardDataModel>?>(
                _httpContext,
                sql,
                new {
                    @BoardType = boardType.ToString(),
                    @LoggerSeq = loggerSeq,
                },
                async (conn, query, parameters) => {
                    var result = (await conn.QueryAsync<ConfigBoardDataModel>(query, parameters, commandTimeout: DBQueryTimeout)).ToList();
                    return result;
                }
            );

        }

        public async Task<ExceptionHandle<List<ChannelConfigDataModel>?>> GetChannelConfigList(ENUM_ChannelType chType) {
            var sql = $@"Select * From tblChannel Where ChType = @ChType Order By ChID ASC;";

            return await QueryDBAsync<List<ChannelConfigDataModel>?>(
                _httpContext,
                sql,
                new {
                    @ChType = (int)chType
                },
                async (conn, query, parameters) => {
                    var result = (await conn.QueryAsync<ChannelConfigDataModel>(query, parameters, commandTimeout: DBQueryTimeout)).ToList();
                    return result;
                }
            );
        }

        // 原因: 舊系統每日 RECID Restart，但是卻沒看到換日時 RECID 重新設定，應該會有 bug
        // 修改: 因為是 ulong, max value 夠大，所以換日不用 reset，直接從 10,000,000,000 起跳
        public async Task<ExceptionHandle<ulong?>> GetMaxRecID(int chType) {

            //var sql = $@"Select max(RecID) from tblRecData Where RecDate=@RecDate And ChType=@ChType;";
            var sql = $@"Select max(RecID) from tblRecData Where ChType=@ChType;";

            return await QueryDBAsync<ulong?>(
                _httpContext,
                sql,
                new {
                    //@RecDate = recDate, <= 移除
                    @ChType = chType,
                },
                async (conn, query, parameters) => {
                    // 用 ulong? => 查不到資料 return NULL，不是 return 0
                    var result = (await conn.QueryAsync<ulong?>(query, parameters, commandTimeout: DBQueryTimeout)).FirstOrDefault();
                    return result;
                }
            );
        }

        public async Task<ExceptionHandle<uint>> GetRecFileCount(DateTime recDate, int chType) {
            var sql = $@"Select count(1) from tblRecData Where RecDate=@RecDate And ChType=@ChType;";

            return await QueryDBAsync<uint>(
                _httpContext,
                sql,
                new {
                    @RecDate = recDate,
                    @ChType = chType,
                },
                async (conn, query, parameters) => {
                    // ulong/uint/int 都是非 nullable 值型別，預設值為 0，所以查不到資料，都是 0
                    var result = (await conn.QueryAsync<uint>(query, parameters, commandTimeout: DBQueryTimeout)).FirstOrDefault();
                    return result;
                }
            );
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="model"></param>
        /// <param name="backupFlag"></param>
        /// <returns>
        /// 1. insert 失敗，return null: 
        /// 2. insert 成功，return 該筆資料的 RecID
        /// </returns>
        public async Task<ExceptionHandle<ulong?>> InsertRecData(RecordingDataModel model, int backupFlag) {
            string sql = $@"Insert Into tblRecData(LoggerSeq, LoggerID, LoggerName, RecID, RecDate, RecFolder, RecFileName, RecFileSizeMB, RecLen, 
                            RecStartTime, RecStopTime, CallType, InboundLen, OutboundLen, CallerID, DTMF, RecType, DNIS, ChType, ChID, ChName, 
                            AgentName, ExtNo, TriggerType, IsSMDR, rev1, rev2, rev3, rev4, rev5, rev6, rev7,
                            BackupFlag, RecGuid) 
                            Values(@LoggerSeq, @LoggerID, @LoggerName, @RecID, @RecDate, @RecFolder, @RecFileName, @RecFileSizeMB, @RecLen, 
                            @RecStartTime, @RecStopTime, @CallType, @InboundLen, @OutboundLen, @CallerID, @DTMF, @RecType, @DNIS, @ChType, @ChID, @ChName, 
                            @AgentName, @ExtNo, @TriggerType, @IsSMDR, @rev1, @rev2, @rev3, @rev4, @rev5, @rev6, @rev7,
                            @BackupFlag, @RecGuid);
                            SELECT SCOPE_IDENTITY()";
            var param = new {
                @LoggerSeq = model.LoggerSeq,
                // @OrgLoggerMac <= 必須故意設為 null, 這樣才會觸發 [trgr_tblRecData_WriteOriginCodec]，
                // 此 Trigger 會自動 Update OriginCodec = (設定的 codec), CodecFlag = 0 
                @LoggerID = model.LoggerID,
                @LoggerName = model.LoggerName,
                @RecID = model.RecID,
                @RecDate = model.RecStartTime.Date,
                @RecFolder = model.NewRecFolder,
                @RecFileName = model.BaseFileName,
                @RecFileSizeMB = model.RecFileSizeMB,
                @RecLen = model.RecLen,
                @RecStartTime = model.RecStartTime,
                @RecStopTime = model.RecStartTime.AddSeconds(model.RecLen),
                @CallType = model.CallType,
                @InboundLen = (model.CallType == 4) ? model.RecLen : 0,
                @OutboundLen = (model.CallType == 5) ? model.RecLen : 0,
                @CallerID = model.CallerID,
                @DTMF = model.DTMF,
                @RecType = model.RecType,
                @DNIS = model.DNIS,
                @ChType = model.ChType,
                @ChID = model.ChNo,
                @ChName = model.ChName,   // Unicode => N'XXXXX 中文會自動轉換
                //@AgentID = model.AgentID, // trgr_tblRecData_WriteOriginCodec 會自動填
                @AgentName = "", // 目前不帶入
                @ExtNo = model.Ext,
                @TriggerType = model.TriggerType,
                @IsSMDR = model.SMDR,
                @rev1 = model.rev1,
                @rev2 = model.DID,
                @rev3 = model.RingLen,
                @rev4 = model.rev4,
                @rev5 = model.rev5,
                @rev6 = model.rev6,
                @rev7 = model.rev7,
                @BackupFlag = backupFlag,       // 0:不需要單機備份, 1: 需要單機備份
                //@OriginCodec = 1,             // 可以 null, 後續 trgr_tblRecData_WriteOriginCodec 會自動更新
                //@CodecFlag = 0,               // default = 0, 後續 trgr_tblRecData_WriteOriginCodec 會自動填
                //@EncryptFlag = 0              // 設為 0 讓系統自行處理，系統會判斷 tblSystemConfig.EncryptRecFile(0/1) 來自動運行，不填 default = 0
                @RecGuid = model.RecGuid
            };
            return await QueryDBAsync<ulong?>(
                _httpContext,
                sql,
                param,
                async (conn, query, parameters) => {
                    var result = (await conn.QueryAsync<ulong>(query, parameters, commandTimeout: DBQueryTimeout)).FirstOrDefault();
                    return result;
                }
            );
        }

        public async Task<ExceptionHandle<int>> UpdateModuleStatus(string moduleID, int status, string moduleVer, long loggerSeq) {
            var sql = $@"IF EXISTS (SELECT * FROM tblModuleStatus WHERE ModuleID=@moduleID)
                            UPDATE tblModuleStatus SET                                                                 
                                Active = 1,
                                Status = @Status, 
                                LastResponse = @LastResponse,                                                                 
                                ModuleVer = @ModuleVer 
                            WHERE ModuleID = @ModuleID And LoggerSeq = @LoggerSeq
                         ELSE
                            INSERT INTO tblModuleStatus(ModuleID, Active, Status, LastResponse, ModuleVer, LoggerSeq) 
                            VALUES(@ModuleID, 1, @Status, @LastResponse, @ModuleVer, @LoggerSeq)";

            return await ExecuteDBAsync<int>(
                _httpContext,
                sql,
                new {                    
                    @LoggerSeq = loggerSeq,
                    @ModuleVer = GVar.CurrentVersion,
                    @LastResponse = DateTime.Now,                    
                    @Status = status,
                    @ModuleID = moduleID
                },
                (conn, sql, parameters) => conn.ExecuteAsync(sql, parameters, commandTimeout: DBQueryTimeout)
            );
        }

        public async Task<ExceptionHandle<int>> UpdateChannelStatus(long loggerSeq, int ursChID, ENUM_LineStatus linStatus, 
                        ENUM_RecordingType recStatus, string callerID, string dtmf, string recGuid = "", DateTime? startTime = null) {

            string cond_guid = "";
            if (!string.IsNullOrEmpty(recGuid))
                cond_guid = $", RecGuid='{recGuid}'";

            string cond_startTime = "";
            if (startTime.HasValue)
                cond_startTime = $", StartTime='{startTime.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            else
                cond_startTime = $", StartTime=null";

            var sql = $@"Update tblChannelStatus with (serializable) Set 
                        LineStatus=@LineStatus, 
                        RecStatus=@RecStatus, 
                        CallerID=@CallerID, 
                        DTMF=@DTMF 
                        {cond_guid} 
                        {cond_startTime}
	                    Where LoggerSeq=@LoggerSeq And 
                              ChID=@ChID";
            return await ExecuteDBAsync<int>(
                _httpContext,
                sql,
                new {
                    @LineStatus = (int)linStatus,
                    @RecStatus = (int)recStatus,
                    @CallerID = callerID,
                    @DTMF = dtmf,
                    @LoggerSeq = loggerSeq,
                    @ChID = ursChID
                },
                (conn, sql, parameters) => conn.ExecuteAsync(sql, parameters, commandTimeout: DBQueryTimeout)
            );
        }

        public async Task<ExceptionHandle<int>> UpdateChannelStatus(ChannelStatusModel model) {
            var sql = $@"Update dbo.tblChannelStatus Set 
                            LineStatus = @LineStatus,
                            RecStatus = @RecStatus,     
                            CallerID = @CallerID,
                            DTMF = @DTMF,
                            TalkType = @TalkType,
                            StartTime = @StartTime,
                            RecGuid = @RecGuid        
                        Where LoggerSeq = @LoggerSeq And 
                              ExtNo = @ExtNo;";

            return await ExecuteDBAsync<int>(
                _httpContext,
                sql,
                model,
                (conn, sql, parameters) => conn.ExecuteAsync(sql, parameters, commandTimeout: DBQueryTimeout)
            );
        }

        public async Task<ExceptionHandle<int>> UpdateRecDataFlags(long recSeq, int backupFlag, int originCodec, int codecFlag, int encryptFlag) {
            var sql = $@"Update tblRecData Set 
                            BackupFlag=@BackupFlag, 
                            OriginCodec=@OriginCodec, 
                            CodecFlag=@CodecFlag, 
                            EncryptFlag=@EncryptFlag 
                        Where RecSeq=@RecSeq";

            return await ExecuteDBAsync<int>(
                _httpContext,
                sql,
                new {
                    @BackupFlag = backupFlag,
                    @OriginCodec = originCodec,
                    @CodecFlag = codecFlag,
                    @EncryptFlag = encryptFlag,
                    @RecSeq = recSeq
                },
                (conn, sql, parameters) => conn.ExecuteAsync(sql, parameters, commandTimeout: DBQueryTimeout)
            );
        }

        public async Task<ExceptionHandle<int>> UpdateRecGuid(long loggerSeq, string extNo, string recGuid) {
            var sql = $@"IF COL_LENGTH('tblChannelStatus', 'RecGuid') IS NOT NULL 
                        BEGIN
	                        Update tblChannelStatus with (serializable) 
	                        Set RecGuid = @RecGuid 
	                        Where ExtNo = @ExtNo and LoggerSeq=@LoggerSeq 
                        END;";

            return await ExecuteDBAsync<int>(
                _httpContext,
                sql,
                new {
                    @RecGuid = recGuid,
                    @ExtNo = extNo,
                    @LoggerSeq = loggerSeq
                },
                (conn, sql, parameters) => conn.ExecuteAsync(sql, parameters, commandTimeout: DBQueryTimeout)
            );
        }
        
    }
}
