using Dapper;
using Microsoft.Data.SqlClient;
using MiniProfiler.Integrations;
using MyProject.lib;
using MyProject.ProjectCtrl;
using NLog;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace MyProject.Database {
    public class _BaseDb {

        public string DBConnStr { internal set; get; } = "";
        public int DBQueryTimeout { internal set; get; } = 15;
        public string DBSchemaName { internal set; get; } = "";

        public CustomDbProfiler DBProf = new CustomDbProfiler();
        protected Logger LOG = LogManager.GetLogger("DatabaseLog");

        public _BaseDb() {
            DBConnStr = GVar.Config == null ? "" : GVar.Config!.DBConnection!.MainDBConnStr!;
            DBConnStr = DecodeDbConnStr(DBConnStr);

            var dbName = GVar.Config == null ? "" : GVar.Config!.DBConnection!.DBName!;
            var schemaName = GVar.Config == null ? "" : GVar.Config!.DBConnection!.SchemaName!;
            DBSchemaName = $"[{dbName}].[{schemaName}].";

            var queryTimeout = GVar.Config == null ? 15 : GVar.Config!.DBConnection!.DBConnectTimeout;
            if (queryTimeout > 15)
                DBQueryTimeout = queryTimeout;
        }

        // Destructor
        ~_BaseDb() {
        }

        public int ReadVarbinaryToFile(string sql, string filePath) {
            int ret = 1;
            try {
                using (SqlConnection conn = new SqlConnection(DBConnStr)) {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn)) {
                        cmd.CommandTimeout = DBQueryTimeout;
                        using (SqlDataReader data = cmd.ExecuteReader()) {
                            if (data.Read()) {
                                byte[] content = (byte[])data[0];
                                System.IO.File.WriteAllBytes(filePath, content);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                LOG.Error($"{lib_misc.GetFunctionName()} exception: {ex.Message}");
                ret = -1;
            }
            return ret;
        }

        public int ReadVarbinaryToByteArray(string sql, ref byte[] byteArray) {
            int ret = 1;
            try {
                using (SqlConnection conn = new SqlConnection(DBConnStr)) {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, conn)) {
                        cmd.CommandTimeout = DBQueryTimeout;
                        using (SqlDataReader data = cmd.ExecuteReader()) {
                            if (data.Read()) {
                                byteArray = (byte[])data[0];
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                LOG.Error($"{lib_misc.GetFunctionName()} exception: {ex.Message}");
                ret = -1;
            }
            return ret;
        }

        private string DecodeDbConnStr(string str) {
            var err = "";
            string key = GVar.DBAesKey; // 32 個英文或數字        
            string iv = GVar.DBAesIV; // 16 個英文或數字

            if (string.IsNullOrEmpty(str) || str.Contains("Data Source")) { // 表示空字串或沒加密
                return str;
            }
            else {
                var decodeStr = lib_encode.DecryptAES256(str, key, iv, out err);
                if (err != "")
                    LOG.Error($"{lib_misc.GetFunctionName()} excetpion: {err}");
                return decodeStr;
            }
        }

        private async Task<int> LogSqlAuditEx2(HttpContext? httpContext, ExceptionHandle errHD, [CallerMemberName] string methodName = "") {
            var remoteUser = httpContext?.User;
            var remoteIpAddress = httpContext?.Connection?.RemoteIpAddress;
            await Task.Delay(1);
            //ExceptionHandle err = new ExceptionHandle();
            var logType = errHD.Success ? ENUM_SqlLogType.Trace : ENUM_SqlLogType.Error;

            if (logType == ENUM_SqlLogType.Trace) {
                if (GVar.Config == null || !GVar.Config!.DBConnection!.SqlTrace)
                    return 0;
            }

            var exeCommand = new List<string>();
            foreach (var cmd in DBProf!.ProfilerContext.ExecutedCommands) {
                exeCommand.Add($"\t CommandType={cmd.CommandType}, DBName={cmd.Database}");
                exeCommand.Add($"\t {cmd.CommandText}");
                exeCommand.Add("\t Parameters:");
                foreach (var p in cmd.Parameters)
                    exeCommand.Add($"\t\t{p}");
            }

            StringBuilder sb = new StringBuilder();

            //TODO: 可以考慮用 token 的 userName 來替代
            sb.Append($"\tremote={remoteUser?.Identity?.Name}@{remoteIpAddress}\r\n");
            sb.Append($"\thostName={Dns.GetHostName()}\r\n");
            sb.Append($"\tsourceIP={GVar.LocalIP}\r\n");
            sb.Append($"\taccessAcct={new SqlConnectionStringBuilder(DBConnStr).UserID}\r\n");
            //sb.Append($"\tcontrollerName={controllerContext.ActionDescriptor.ControllerName}\r\n");
            sb.Append($"\tmethodName={methodName}\r\n");
            sb.Append("###ExecutedCommands =>\r\n");
            sb.Append($"{string.Join('\n', exeCommand)}\r\n");
            sb.Append("###FailedCommands =>\r\n");
            sb.Append($"{DBProf.ProfilerContext.GetFailedCommands()}\r\n");

            if (logType == ENUM_SqlLogType.Trace)
                LOG.Info(sb.ToString());
            else {
                LOG.Error(errHD.UserMessage);
                LOG.Error(sb.ToString());
            }
            return 1;
        }

        // For WEB Only
        //public PageInfo GetTotalRecords(List<object> list, PageInfo pageInfo) {
        //    if (list == null || list.Count == 0) {
        //        pageInfo.TotalRecords = 0;
        //        return pageInfo;
        //    }

        //    try {
        //        var firstItem = list[0];
        //        if (firstItem == null) {
        //            pageInfo.TotalRecords = 0;
        //            return pageInfo;
        //        }

        //        var propertyInfo = firstItem.GetType().GetProperty("TotalCount");
        //        if (propertyInfo != null) {
        //            var value = propertyInfo.GetValue(firstItem, null);
        //            // 若取得的值非 long 型態，嘗試轉換
        //            pageInfo.TotalRecords = value is long ? (long)value : Convert.ToInt64(value);
        //        }
        //        else {
        //            pageInfo.TotalRecords = 0;
        //        }
        //    }
        //    catch {
        //        pageInfo.TotalRecords = 0;
        //    }

        //    return pageInfo;
        //}


        public async Task<ExceptionHandle<T>> QueryDBAsync<T>(HttpContext? httpContext, string sql, object? parameters,
                                        Func<IDbConnection, string, object?, Task<T>> queryFunc) {
            var errHD = new ExceptionHandle<T>();
            try {
                DBProf.ProfilerContext.Reset();
                using (var conn = ProfiledDbConnectionFactory.New(new SqlServerDbConnectionFactory(DBConnStr), DBProf)) {
                    if (conn.State != ConnectionState.Open)
                        await ((DbConnection)conn).OpenAsync();

                    // 執行傳入的 lambda 表達式，取得查詢結果
                    errHD.Data = await queryFunc(conn, sql, parameters);
                }
            }
            catch (Exception ex) {
                errHD.ParseError(ex);
                errHD.Data = default;
            }
            LogSqlAuditEx2(httpContext, errHD);
            return errHD;
        }

        public async Task<ExceptionHandle<T>> QueryDBAsync<T>(
            HttpContext httpContext,
            string dbConnStr,
            string sql,
            object? parameters,
            Func<IDbConnection, string, object?, Task<T>> queryFunc) {
            var errHD = new ExceptionHandle<T>();
            try {
                DBProf.ProfilerContext.Reset();
                using (var conn = ProfiledDbConnectionFactory.New(new SqlServerDbConnectionFactory(dbConnStr), DBProf)) {
                    errHD.Data = await queryFunc(conn, sql, parameters);
                }
            }
            catch (Exception ex) {
                errHD.ParseError(ex);
                errHD.Data = default;
            }
            LogSqlAuditEx2(httpContext, errHD);
            return errHD;
        }


        public async Task<ExceptionHandle<T>> MultipleQueryDBAsync<T>(HttpContext? httpContext, string sql, object? parameters,
                    Func<SqlMapper.GridReader, Task<T>> processFunc) {
            var errHD = new ExceptionHandle<T>();
            try {
                DBProf.ProfilerContext.Reset();
                using (var conn = ProfiledDbConnectionFactory.New(new SqlServerDbConnectionFactory(DBConnStr), DBProf)) {
                    using (var grid = await conn.QueryMultipleAsync(sql, parameters)) {
                        errHD.Data = await processFunc(grid);
                    }
                }
            }
            catch (Exception ex) {
                errHD.ParseError(ex);
                errHD.Data = default;
            }
            LogSqlAuditEx2(httpContext, errHD);
            return errHD;
        }

        // 不管SQL 執行失敗、或資料沒有新增/更新成功，都會回傳 0(應該是 T.default，int/long 的 default = 0 )
        public async Task<ExceptionHandle<T>> ExecuteDBAsync<T>(HttpContext? httpContext, string sql, object parameters,
                    Func<IDbConnection, string, object, Task<T>> executeFunc) {
            var errHD = new ExceptionHandle<T>();
            try {
                DBProf.ProfilerContext.Reset();
                using (var conn = ProfiledDbConnectionFactory.New(new SqlServerDbConnectionFactory(DBConnStr), DBProf)) {
                    errHD.Data = await executeFunc(conn, sql, parameters);
                }
            }
            catch (Exception ex) {
                errHD.ParseError(ex);
                errHD.Data = default;
            }
            LogSqlAuditEx2(httpContext, errHD);
            return errHD;
        }

        // 外部帶入 conn, tran，適合在 Transaction 中使用
        public async Task<ExceptionHandle<T>> ExecuteDBAsyncTrans<T>(HttpContext? httpContext, IDbConnection conn, IDbTransaction tran, string sql,
                object parameters, Func<IDbConnection, string, object, IDbTransaction, Task<T>> executeFunc) {

            var errHD = new ExceptionHandle<T>();
            try {
                DBProf.ProfilerContext.Reset();
                errHD.Data = await executeFunc(conn, sql, parameters, tran);
            }
            catch (Exception ex) {
                errHD.ParseError(ex);
                errHD.Data = default;
            }
            LogSqlAuditEx2(httpContext, errHD);
            return errHD;
        }

        public string GetCurrentMethod() {
            return new StackTrace()?.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "";
        }

    }
}