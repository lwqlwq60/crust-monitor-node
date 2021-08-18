using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
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

        private const string DiskDir = "/opt/crust/disks";

        private static Container ChainContainer = new Container();
        private static Container ApiContainer = new Container();
        private static Container SWorkerContainer = new Container();
        private static Container SWorkerAContainer = new Container();
        private static Container SWorkerBContainer = new Container();
        private static Container SManagerContainer = new Container();
        private static Container IpfsContainer = new Container();
        private static Random Random = new Random();

        private static volatile Dictionary<string, Container> CrustContainers =
            new Dictionary<string, Container>
            {
                { "chain", ChainContainer },
                { "api", ApiContainer },
                { "sworker", SWorkerContainer },
                { "sworker-a", SWorkerAContainer },
                { "sworker-b", SWorkerBContainer },
                { "smanager", SManagerContainer },
                { "ipfs", IpfsContainer }
            };

        public CrustMonitorController(HttpClient httpClient, DockerClient dockerClient)
        {
            _httpClient = httpClient;
            _dockerClient = dockerClient;
        }

        [HttpGet("workload")]
        public async Task<IActionResult> GetWorkloadAsync()
        {
            var result = await _httpClient.GetStringAsync("http://host.docker.internal:12222/api/v0/workload");
            return Content(result);
        }

        [HttpGet("disk-status")]
        public IActionResult GetDiskStatus()
        {
            var disks = Directory.GetDirectories(DiskDir).ToDictionary(_ => _, _ => new DiskStatus());
            foreach (var disk in disks.Keys)
            {
                var drive = new DriveInfo(disk);
                disks[disk].Available = Math.Round(drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 2);
                disks[disk].Total = Math.Round(drive.TotalSize / 1024.0 / 1024.0 / 1024.0, 2);
                disks[disk].Ready = drive.IsReady;
                disks[disk].VolumeLabel = drive.VolumeLabel;
            }

            return Json(disks);
        }

        [HttpGet("disk-status/{id}")]
        public IActionResult GetDiskStatus(string id)
        {
            var drive = new DriveInfo($"{DiskDir}/{id}");
            var status = new DiskStatus
            {
                Available = Math.Round(drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0, 1),
                Total = Math.Round(drive.TotalSize / 1024.0 / 1024.0 / 1024.0, 1),
                Ready = drive.IsReady,
                VolumeLabel = drive.VolumeLabel
            };

            return Json(status);
        }

        [HttpGet("chain-logs/{limit}")]
        public async Task<IActionResult> GetChainLogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(ChainContainer.Id, limit);
        }

        [HttpGet("api-logs/{limit}")]
        public async Task<IActionResult> GetApiLogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(ApiContainer.Id, limit);
        }

        [HttpGet("sworker-logs/{limit}")]
        public async Task<IActionResult> GetSWorkerLogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(SWorkerContainer.Id, limit);
        }

        [HttpGet("sworker-a-logs/{limit}")]
        public async Task<IActionResult> GetSWorkerALogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(SWorkerAContainer.Id, limit);
        }

        [HttpGet("sworker-b-logs/{limit}")]
        public async Task<IActionResult> GetSWorkerBLogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(SWorkerBContainer.Id, limit);
        }


        [HttpGet("smanager-logs/{limit}")]
        public async Task<IActionResult> GetSManagerLogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(SManagerContainer.Id, limit);
        }

        [HttpGet("ipfs-logs/{limit}")]
        public async Task<IActionResult> GetIpfsLogsAsync(string limit)
        {
            await CheckContainersAsync();
            return await GetLogsAsync(IpfsContainer.Id, limit);
        }


        private async Task<IActionResult> GetLogsAsync(string id, string lines)
        {
            if (string.IsNullOrEmpty(id))
                return NoContent();
            var logs = await _dockerClient.Containers.GetContainerLogsAsync(id, false,
                new ContainerLogsParameters { ShowStdout = true, ShowStderr = true, Tail = lines });
            var (stdout, stderr) = await logs.ReadOutputToEndAsync(CancellationToken.None);
            Console.WriteLine(stderr);
            return Content(stdout);
        }

        private async Task CheckContainersAsync()
        {
            var containers =
                await _dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
            foreach (var container in containers)
            {
                if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/crust")
                {
                    ChainContainer.Status = container.Status;
                    ChainContainer.Id = container.ID;
                }
                else if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/crust-api")
                {
                    ApiContainer.Status = container.Status;
                    ApiContainer.Id = container.ID;
                }
                else if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/crust-sworker")
                {
                    SWorkerContainer.Status = container.Status;
                    SWorkerContainer.Id = container.ID;
                }
                else if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/crust-sworker-a")
                {
                    SWorkerAContainer.Status = container.Status;
                    SWorkerAContainer.Id = container.ID;
                }
                else if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/crust-sworker-b")
                {
                    SWorkerBContainer.Status = container.Status;
                    SWorkerBContainer.Id = container.ID;
                }
                else if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/crust-smanager")
                {
                    SManagerContainer.Status = container.Status;
                    SManagerContainer.Id = container.ID;
                }
                else if (!container.Names.IsNullOrEmpty() && container.Names.First() == "/ipfs")
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