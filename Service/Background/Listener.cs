using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;

namespace EdcsServer.Service.Background
{
    public class Listener : BackgroundService
    {
        private readonly IRabbitService _service;
        public Listener(IRabbitService service)
        {
            _service = service; 
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.Write(".");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
