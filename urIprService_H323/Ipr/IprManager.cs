using MyProject.ProjectCtrl;
using Synway.Models;
using System.Collections.Concurrent;

namespace IprService.Ipr {
    public class IprManager {
        private readonly ConcurrentDictionary<int, IprWorker> _workers = new();

        public void CreateIprWorker(int hwChID, ENUM_IprChType iprType) {
            var worker = new IprWorker(hwChID, iprType);
            if (_workers.TryAdd(hwChID, worker)) {
                worker.Start();
            }
        }

        public void PushData(int hwChID, SsmEventDataModel ssmModel) {
            if (_workers.TryGetValue(hwChID, out var worker)) {
                worker.EnqueueData(ssmModel);
            }
        }

        public void RemoveIprWorker(int hwChID) {
            if (_workers.TryRemove(hwChID, out var worker)) {
                worker.Stop();
            }
        }
    }
}