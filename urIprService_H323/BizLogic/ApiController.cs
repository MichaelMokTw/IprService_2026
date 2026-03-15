using System.Reflection;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MyProject.ProjectCtrl;

namespace MyProject.Controllers {
    [ApiController]
    [Route("api/[action]")]
    public class ApiController : ControllerBase
    {
        private IConfiguration _configuration;
        private IWebHostEnvironment _webHostEnvironment;

        public ApiController(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Echo() {            
            await Task.Delay(1); 
            var info = new {
                GlobalVar.AppSettings!.ServerID,
                GlobalVar.AppSettings.AppID,
                ApiVersion = GlobalVar.CurrentVersion,
                ServerIP = GlobalVar.LocalIP,                
                Project = GlobalVar.ProjectName,
                _webHostEnvironment.EnvironmentName,
                Config_RecvPacket = GlobalVar.AppSettings.RecvPacket,                
                Config_WebAPI = GlobalVar.AppSettings.WebAPI,
            };
            return Ok(info);            
        }

        [HttpGet]
        public async Task<IActionResult> ChannelInfo() {
            await Task.Delay(1);
            var infoList = new List<dynamic>();
            foreach(var thd in GlobalVar.DictProcessThread) {
                var info = thd.Value.GetThreadInfo();
                infoList.Add(info);
            }            
            return Ok(infoList);
        }
    }
}
