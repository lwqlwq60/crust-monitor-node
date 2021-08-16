using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using CrustMonitorNode.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Mvc;

namespace CrustMonitorNode.Controllers
{
    [ApiController]
    [Route("/api/crust-monitor")]
    public class CrustMonitorController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly DockerClient _dockerClient;

        private const string CrustDir = "/opt/crust";
        private const string Chain = "crust";

        private static Container ChainContainer = new Container();
        private static Container ApiContainer = new Container();
        private static Container SWorkerContainer = new Container();
        private static Container SManagerContainer = new Container();
        private static Container IpfsContainer = new Container();

        private static volatile Dictionary<string, Container> CrustContainers =
            new Dictionary<string, Container>
            {
                {"chain", ChainContainer},
                {"api", ApiContainer},
                {"sworker", SWorkerContainer},
                {"smanager", SManagerContainer},
                {"ipfs", IpfsContainer}
            };

        public CrustMonitorController(HttpClient httpClient, DockerClient dockerClient)
        {
            _httpClient = httpClient;
            _dockerClient = dockerClient;
        }

        // [HttpGet("chain-logs")]
        // public Task<IActionResult> GetChainLogsAsync()
        // {
        //     
        // }

        private async Task CheckContainersAsync()
        {
            var containers =
                await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters {All = true});
            foreach (var container in containers)
            {
                if (container.Image == "crustio/crust:latest")
                {
                    ChainContainer.Status = container.Status;
                    ChainContainer.Id = container.ID;
                }
                else if (container.Image == "crustio/crust-api:latest")
                {
                    ApiContainer.Status = container.Status;
                    ApiContainer.Id = container.ID;
                }
                else if (container.Image == "crustio/crust-sworker:latest")
                {
                    SWorkerContainer.Status = container.Status;
                    SWorkerContainer.Id = container.ID;
                }
                else if (container.Image == "crustio/crust-smanager:latest")
                {
                    SManagerContainer.Status = container.Status;
                    SManagerContainer.Id = container.ID;
                }
                else if (container.Image == "crustio/go-ipfs:latest")
                {
                    IpfsContainer.Status = container.Status;
                    IpfsContainer.Id = container.ID;
                }
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatusAsync()
        {
            await CheckContainersAsync();
            return Json(CrustContainers);
        }

        [HttpGet("health")]
        public IActionResult CheckHealth() => Ok();
    }
}