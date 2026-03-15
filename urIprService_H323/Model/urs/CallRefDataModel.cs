using MyProject.ProjectCtrl;
using System.Collections.Concurrent;

namespace urIprService.Models {

    public enum ENUM_CallRefStatus {
        CallConnected = 0,
        CallHeld = 1,
        CallReleased = 2
    };

    public enum ENUM_RecordingStatus {
        Stop = 0,
        Start = 1,
        Pause = 2
    };

    public class CallRefDataModel {
        public uint CallRef { get; init; } // 使用 init 確保 ID 不會被中途竄改
        public ENUM_CallDir CallDir { get; set; }
        public DateTime? SessionStartTime { get; set; } = null;
        public ENUM_RecordingStatus RecStatus { get; set; } = ENUM_RecordingStatus.Stop;
        public string CallerID { get; set; } = string.Empty;
        public string DTMF { get; set; } = string.Empty;
        public ENUM_CallRefStatus Status { get; set; } = ENUM_CallRefStatus.CallConnected;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime? LastHeldTime { get; set; }

        public CallRefDataModel(uint callRef) {
            CallRef = callRef;
        }
    }

    public class CallRefManager {
        // 如果是單執行緒環境用 Dictionary<uint, CallRefDataModel> 即可
        // 如果是多執行緒環境（如之前的 Worker 機制），務必用 ConcurrentDictionary
        private readonly ConcurrentDictionary<uint, CallRefDataModel> _dicCallRef = new();

        // 回傳所有 CallRef (取代原本的 List 屬性)
        public IEnumerable<CallRefDataModel> CallRefList => _dicCallRef.Values;
        
        public int CallRefCount => _dicCallRef.Count;

        // 如果你想判斷是否為空，這個效能比 Count == 0 更快
        public bool IsCallRefEmpty => _dicCallRef.IsEmpty;

        // 優化後的 Add: 使用 GetOrAdd 一行搞定
        public CallRefDataModel AddCallRef(uint callRef) {
            return _dicCallRef.GetOrAdd(callRef, (key) => new CallRefDataModel(key));
        }

        // 優化後的 Get: Dictionary 查詢速度遠快於 List.Where
        public CallRefDataModel? GetCallRef(uint callRef) {
            return _dicCallRef.TryGetValue(callRef, out var obj) ? obj : null;
        }

        public void RemoveCallRef(uint callRef) {
            _dicCallRef.TryRemove(callRef, out _);
        }

        // 優化後的只保留一個：清空後重新加入
        public void RemoveOtherCallRef(uint keepCallRef) {
            if (_dicCallRef.TryRemove(keepCallRef, out var keepObj)) {
                _dicCallRef.Clear();
                _dicCallRef.TryAdd(keepCallRef, keepObj);
            }
            else {
                _dicCallRef.Clear();
            }
        }

        /*
        // 檢查是否有不合法的 callRefObj：
        // 正常狀況:   DE_CALL_CONNECTED => E_RCV_IPR_MEDIA_SESSION_STARTED
        //            => 經過 E_RCV_IPR_MEDIA_SESSION_STARTED，必然會指定 SessionTime   
        // 不正常狀況: DE_CALL_CONNECTED => 但沒有 E_RCV_IPR_MEDIA_SESSION_STARTED
        //            => 沒有 E_RCV_IPR_MEDIA_SESSION_STARTED，SessionTime = DateTime.MinValue
        // ***********************************************************************************
        // 如果有不正常的 callRefObj，就先移除，以確保能正常停止錄音，
        // 但是上一通會跟這一通連在一起，這是難免，但畢竟少數
        // ***********************************************************************************
        */
        public void CleanupInvalidRefs(int sessionTimeoutSec, int maxHoldMin) {
            var now = DateTime.Now;

            // ConcurrentDictionary 可以在巡覽時安全移除項目
            foreach (var kvp in _dicCallRef) {
                var callRef = kvp.Value;
                bool isSessionMissing = callRef.SessionStartTime == DateTime.MinValue &&
                                        (now - callRef.CreatedTime).TotalSeconds > sessionTimeoutSec;

                bool isHeldTimeout = callRef.Status == ENUM_CallRefStatus.CallHeld &&
                                     callRef.LastHeldTime.HasValue &&
                                     (now - callRef.LastHeldTime.Value).TotalMinutes > maxHoldMin;

                if (isSessionMissing || isHeldTimeout) {
                    // 使用 TryRemove 確保移除的是當前這一個物件
                    _dicCallRef.TryRemove(kvp.Key, out _);
                }
            }
        }

        public void ClearAll() {
            _dicCallRef.Clear();         
        }
    }


}
