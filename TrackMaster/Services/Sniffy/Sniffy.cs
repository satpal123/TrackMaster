using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using SharpPcap.Npcap;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TrackMaster.Hubs;

namespace TrackMaster.Services.Sniffy
{
    public class Sniffy : BackgroundService
    {
        #region Variable declarations
        public static string appfullpath;
        private readonly IHubContext<TrackistHub> _tracklisthubContext;
        private readonly string _ethernetdevice;
        private readonly string _controllerIP;
        private static string playerstatus;
        public static int globalplayernumber1;
        public static string globalplayerstatus1;
        public static string globalplayerloadeddevice1;
        public static string globalplayermaster1;
        public static string globalplayerfader1;
        public static int globalplayernumber2;
        public static string globalplayerstatus2;
        public static string globalplayerloadeddevice2;
        public static string globalplayermaster2;
        public static string globalplayerfader2;
        private static int deviceloadedfrom;
        private static string loadeddevice;
        private static string playermaster;
        private static string globalrekordboxid1;
        private static Tuple<int, string> deck1;
        private static string globalrekordboxid2;
        private static Tuple<int, string> deck2;
        private static int countrbidoccurance1;
        private static int countrbidoccurance2;
        private static int trackpathid;
        private static int _player2;
        private static int _player1;
        public static string trackpath;
        private static int t;
        private static int u;
        public static string trackpath2;
        private static string fadermaster;
        public static string playername;
        private static int trackTitle1;
        private static int trackTitle2;
        private static string tracktitle1;
        private static string trackartist1;
        private static string tracktitle2;
        private static string trackartist2;
        private int playernumber;
        private static string currentpcid;
        private int j = 0;
        private readonly ILogger _logger;
        public static bool ControllerFound { get; private set; } = false;
        public static string ControllerIP { get; private set; }
        #endregion
        public Sniffy(IConfiguration configuration, IHubContext<TrackistHub> synchub, ILogger<Sniffy> logger)
        {
            _tracklisthubContext = synchub;
            _ethernetdevice = configuration.GetSection("EthernetDevice").Value;
            _controllerIP = configuration.GetSection("ControllerIP").Value;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() =>
                Console.WriteLine($"StatusCheckerService background task is stopping."));
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (ControllerFound == false)
                    {
                        await CapturePacketsInitialAsync();

                    }
                    await CapturePacketsAsync();                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            Console.WriteLine($"StatusCheckerService background task is stopping.");
        }
        private async Task CapturePacketsInitialAsync()
        {
            j = await GetConnectedControllerIPAsync();
        }
        public async Task CapturePacketsAsync()
        {
                await Task.Run(() =>
                {
                    // Retrieve the device list
                    var devices = CaptureDeviceList.Instance;

                    // If no devices were found print an error
                    if (devices.Count < 1)
                    {
                        Console.WriteLine("No devices were found on this machine");
                        _logger.LogError("No devices were found on this machine");

                        _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 2, "No devices were found on this machine");
                        return;
                    }

                    int i = 0;

                    i = int.Parse(j.ToString());

                    var device = devices[i];

                    // Register our handler function to the 'packet arrival' event
                    device.OnPacketArrival +=
                       new PacketArrivalEventHandler(device_OnPacketArrivalUdp);
                    device.OnPacketArrival +=
                        new PacketArrivalEventHandler(device_OnPacketArrivalTcp);

                    // Open the device for capturing
                    int readTimeoutMilliseconds = 1000;

                    if (device is NpcapDevice)
                    {
                        var nPcap = device as NpcapDevice;
                        nPcap.Open(OpenFlags.DataTransferUdp | OpenFlags.NoCaptureLocal, readTimeoutMilliseconds);
                    }
                    else if (device is LibPcapLiveDevice)
                    {
                        var livePcapDevice = device as LibPcapLiveDevice;
                        livePcapDevice.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
                    }
                    else
                    {
                        _logger.LogError("unknown device type of " + device.GetType().ToString());
                        throw new InvalidOperationException("unknown device type of " + device.GetType().ToString());
                    }

                    Console.WriteLine("Capture Started on " + device.Description);
                    // Start the capturing process

                    device.StartCapture();

                    Console.ReadLine();
                });
                
        }
        private async Task<int> GetConnectedControllerIPAsync()
        {
            // Retrieve the device list
            var devices = CaptureDeviceList.Instance;

            int i=0;

            while (!ControllerFound)
            {
                i = 0;
                // Print out the devices
                foreach (var dev in devices)
                {
                    if (!dev.Description.Contains("loop"))
                    {
                        Console.WriteLine("{0}) {1} {2}", i, dev.Name, dev.Description);

                        i = int.Parse(i.ToString());

                        await AutoConfigureAsync(i, devices);

                        if (ControllerFound)
                            break;

                        i++;
                    }                    
                }
            }
            return i;
        }
        private async Task AutoConfigureAsync(int i, CaptureDeviceList devices)
        {
            var device = devices[i];

            await _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 3, "Searching for Device...");

            // Register our handler function to the 'packet arrival' event
            device.OnPacketArrival +=
               new PacketArrivalEventHandler(device_OnPacketArrivalUdpInitial);

            // Open the device for capturing
            int readTimeoutMilliseconds = 1000;
            if (device is NpcapDevice)
            {
                var nPcap = device as NpcapDevice;
                nPcap.Open(OpenFlags.DataTransferUdp | OpenFlags.NoCaptureLocal, readTimeoutMilliseconds);
            }
            else if (device is LibPcapLiveDevice)
            {
                var livePcapDevice = device as LibPcapLiveDevice;
                livePcapDevice.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);
            }
            else
            {
                throw new InvalidOperationException("unknown device type of " + device.GetType().ToString());
            }

            // Start the capturing process
            device.StartCapture();

            await Task.Delay(4000);
            device.StopCapture();
            device.Close();
        }
        private void device_OnPacketArrivalUdpInitial(object sender, CaptureEventArgs e)
        {
            currentpcid = GetLocalIPAddress();

            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            var udpPacket = packet.Extract<UdpPacket>();

            if (udpPacket != null)
            {
                var ipPacket = (IPPacket)udpPacket.ParentPacket;
                IPAddress srcIp = ipPacket.SourceAddress;             
                int dstPort = udpPacket.DestinationPort;

                if (ipPacket.PayloadPacket.PayloadDataSegment != null)
                {
                    var magicnumber = ipPacket.PayloadPacket.PayloadDataSegment.Bytes.Skip(42).ToArray();

                    var result = BitConverter.ToString(magicnumber).Replace("-", string.Empty);

                    if (dstPort == 50000 & result.StartsWith("5173707431576D4A4F4C06"))
                    {
                        if (srcIp.ToString() != currentpcid)
                        {
                            ControllerFound = true;
                            ControllerIP = srcIp.ToString();

                            string test = BitConverter.ToString(magicnumber[44..48]).Replace("-", string.Empty);

                            var y = IPAddress.HostToNetworkOrder(int.Parse(test, NumberStyles.HexNumber)).ToString("X");

                            var ip = new IPAddress(long.Parse(y, NumberStyles.AllowHexSpecifier));
                        }
                    }
                }
            }
        }
        private void device_OnPacketArrivalTcp(object sender, CaptureEventArgs e)
        {
            try
            {
                //globalplayernumber1 = 11; globalplayerfader1 = "Fader open"; globalplayerstatus1 = "Player is playing normally"; trackpath = "Test";

                var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

                var tcpPacket = packet.Extract<TcpPacket>();

                if (tcpPacket != null)
                {
                    var ipPacket = (IPPacket)tcpPacket.ParentPacket;

                    var magicnumber = ipPacket.PayloadPacket.PayloadDataSegment.Bytes.Skip(54).ToArray();

                    var result = BitConverter.ToString(magicnumber).Replace("-", string.Empty);

                    if (result.Contains("11872349AE11"))
                    {
                        var identity = Regex.Matches(result, "1021020F02140000000C060600000000000000000000");

                        var identity2 = Regex.Matches(result, "1041010F0C140000000C060606020602060606060606");

                        var metadataidentity = "1041010F0C140000000C060606020602060606060606";

                        if (identity.Count == 1)
                        {
                            playernumber = Convert.ToInt32(BitConverter.ToString(magicnumber[33..34]), 16);
                            var rbid = result.Substring(76, 8);

                            if (playernumber == 11)
                            {
                                if (globalrekordboxid1 != rbid)
                                {
                                    t = 0;
                                }

                                globalrekordboxid1 = rbid;
                                deck1 = Tuple.Create(playernumber, rbid);

                            }
                            if (playernumber == 12)
                            {
                                if (globalrekordboxid2 != rbid)
                                {
                                    u = 0;
                                }
                                globalrekordboxid2 = rbid;
                                deck2 = Tuple.Create(playernumber, rbid);
                            }
                        }

                        if (identity2.Count >= 10)
                        {
                            if (globalrekordboxid1 != null)
                            {
                                countrbidoccurance1 = Regex.Matches(result, globalrekordboxid1).Count;
                                if (countrbidoccurance1 != 0)
                                {
                                    if (countrbidoccurance1 >= 2)
                                    {
                                        trackTitle1 = result.IndexOf(metadataidentity, result.IndexOf(metadataidentity));
                                    }                                   
                                }

                                _player1 = 11;
                            }
                            if (globalrekordboxid2 != null)
                            {
                                countrbidoccurance2 = Regex.Matches(result, globalrekordboxid2).Count;
                                if (countrbidoccurance2 != 0)
                                {
                                    if (countrbidoccurance2 >= 2)
                                    {
                                        trackTitle2 = result.IndexOf(metadataidentity, result.IndexOf(metadataidentity));
                                    }                                  
                                }

                                _player2 = 12;
                            }

                            if (countrbidoccurance1 >= 1)
                            {
                                t++;
                                if (t == 1)
                                {
                                    if (_player1 == 11)
                                    {
                                        var title = result[trackTitle1..].Split(metadataidentity)[1].Split("11000000")[2];
                                        var artist = result[trackTitle1..].Split(metadataidentity)[2].Split("11000000")[2];

                                        trackartist1 = (artist != "00") ? HextoString(artist[14..]).Replace("\0", string.Empty).Trim() : "";
                                        tracktitle1 = (title != "00") ? HextoString(title[14..]).Replace("\0", string.Empty).Trim() : "" ;

                                        if (tracktitle1 != "" & trackartist1 != "")
                                        {
                                            trackpath = trackartist1.Remove(trackartist1.Length - 1) + " - " + tracktitle1.Remove(tracktitle1.Length - 1);
                                        }
                                        else if (tracktitle1 != "" & trackartist1 == "")
                                        {
                                            trackpath = tracktitle1.Remove(tracktitle1.Length - 1);
                                        }
                                        else
                                        {
                                            trackpath = "ID - ID";
                                        }                                        

                                        _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 5, trackpath);

                                        t = 4;
                                    }
                                }
                            }

                            if (countrbidoccurance2 >= 1)
                            {
                                u++;
                                if (u == 1)
                                {
                                    if (_player2 == 12)
                                    {
                                        var title = result[trackTitle2..].Split(metadataidentity)[1].Split("11000000")[2];
                                        var artist = result[trackTitle2..].Split(metadataidentity)[2].Split("11000000")[2];

                                        trackartist2 = (artist != "00") ? HextoString(artist[14..]).Replace("\0", string.Empty).Trim() : "";
                                        tracktitle2 = (title != "00") ? HextoString(title[14..]).Replace("\0", string.Empty).Trim() : "";

                                        if (tracktitle2 != "" & trackartist2 != "")
                                        {
                                            trackpath2 = trackartist2.Remove(trackartist2.Length - 1) + " - " + tracktitle2.Remove(tracktitle2.Length - 1);
                                        }
                                        else if (tracktitle2 != "" & trackartist2 == "")
                                        {
                                            trackpath2 = tracktitle2.Remove(tracktitle2.Length - 1);
                                        }
                                        else
                                        {
                                            trackpath2 = "ID - ID";
                                        }

                                        _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 5, trackpath2);
                                        u = 4;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }            
        }        
        private void device_OnPacketArrivalUdp(object sender, CaptureEventArgs e)
        {            
            var packet = Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

            var udpPacket = packet.Extract<UdpPacket>();

            if (udpPacket != null)
            {
                var ipPacket = (IPPacket)udpPacket.ParentPacket;
                IPAddress srcIp = ipPacket.SourceAddress;
                int dstPort = udpPacket.DestinationPort;

                if (srcIp.ToString() == ControllerIP)
                {
                    var magicnumber = ipPacket.PayloadPacket.PayloadDataSegment.Bytes.Skip(42).ToArray();

                    var result = BitConverter.ToString(magicnumber).Replace("-", string.Empty);

                    if (srcIp.ToString() == ControllerIP & dstPort == 50002 & result.StartsWith("5173707431576D4A4F4C"))
                    {
                        playername = Encoding.Default.GetString(magicnumber[11..24]).Replace("\0", string.Empty);
                        var playernumber = Convert.ToInt32(BitConverter.ToString(magicnumber[33..34]), 16);

                        if (result.StartsWith("5173707431576D4A4F4C0A"))
                        {
                            _tracklisthubContext.Clients.All.SendAsync("DeviceAndTwitchStatus", 1, playername + " (" + ControllerIP + ")");

                            deviceloadedfrom = Convert.ToInt32(BitConverter.ToString(magicnumber[41..42]), 16);

                            loadeddevice = deviceloadedfrom switch
                            {
                                04 => "RekordBox",
                                03 => "USB",
                                _ => "None",
                            };

                            var currentplaymode = Convert.ToInt32(BitConverter.ToString(magicnumber[123..124]), 16);

                            playerstatus = currentplaymode switch
                            {
                                00 => "No track is loaded",
                                02 => "A track is in the process of loading",
                                03 => "Player is playing normally",
                                04 => "Player is playing a loop",
                                05 => "Player is paused anywhere other than the cue point",
                                06 => "Player is paused at the cue point",
                                07 => "Cue Play is in progress",
                                08 => "Cue scratch is in progress",
                                09 => "Player is searching forwards or backwards",
                                17 => "Player reached the end of the track and stopped",
                                _ => ""
                            };

                            var master = Convert.ToInt32(BitConverter.ToString(magicnumber[158..159]), 16);

                            switch (master)
                            {
                                case 00:
                                    playermaster = "None";
                                    break;
                                case 01:
                                    playermaster = "Tempo Master";
                                    break;
                                case 02:
                                    playermaster = "Master";
                                    break;
                            }

                            var faderstatus = Convert.ToInt32(BitConverter.ToString(magicnumber[38..39]), 16);

                            switch (faderstatus)
                            {
                                case 00:
                                    fadermaster = "Fader closed";
                                    break;
                                case 01:
                                    fadermaster = "Fader open";
                                    break;
                            }
                        }
                        else
                        {
                            playerstatus = "";
                        }

                        if (playernumber == 11)
                        {
                            globalplayernumber1 = playernumber;
                            globalplayerstatus1 = playerstatus;
                            globalplayerloadeddevice1 = loadeddevice;
                            globalplayermaster1 = playermaster;
                            globalplayerfader1 = fadermaster;

                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 1, globalplayerstatus1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 2, globalplayerloadeddevice1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 3, globalplayermaster1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 4, globalplayerfader1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 5, trackpath);
                        }
                        if (playernumber == 12)
                        {
                            globalplayernumber2 = playernumber;
                            globalplayerstatus2 = playerstatus;
                            globalplayerloadeddevice2 = loadeddevice;
                            globalplayermaster2 = playermaster;
                            globalplayerfader2 = fadermaster;

                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 1, globalplayerstatus2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 2, globalplayerloadeddevice2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 3, globalplayermaster2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 4, globalplayerfader2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 5, trackpath2);
                        }
                    }
                }                
            }
        }
        public static string GetLocalIPAddress()
        {
            //var host = Dns.GetHostEntry(Dns.GetHostName());
            //foreach (var ip in host.AddressList)
            //{
            //    if (ip.AddressFamily == AddressFamily.InterNetwork)
            //    {
            //        return ip.ToString();
            //    }
            //}
            //throw new Exception("No network adapters with an IPv4 address in the system!");

            var GetIP = "";

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            GetIP = ip.Address.ToString();
                        }
                    }
                }                
            }

            return GetIP;

        }
        public static string HextoString(string InputText)
        {

            byte[] bb = Enumerable.Range(0, InputText.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(InputText.Substring(x, 2), 16))
                             .ToArray();
            return Encoding.Unicode.GetString(bb);
        }
    }
}
