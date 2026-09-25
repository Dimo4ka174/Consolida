using Application.DataAccessLayer.Interface.Common;

namespace Application.DataAccessLayer.Jobs
{
    public class LockCleanupJob
    {
        private readonly ILockService _lockService;

        public LockCleanupJob(ILockService lockService)
        {
            _lockService = lockService;
        }

        public async Task ExecuteAsync()
        {
            await _lockService.ReleaseExpiredLocksAsync();
        }
    }
}
