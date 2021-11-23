using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TrackMaster.Helper;
using TrackMaster.Hubs;
using TrackMaster.Models;

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
        public static string globalplayerstatus1, globalplayerloadeddevice1, globalplayermaster1, globalplayerfader1;
        public static int globalplayernumber2;
        public static string globalplayerstatus2, globalplayerloadeddevice2, globalplayermaster2, globalplayerfader2;
        private static int deviceloadedfrom;
        private static string loadeddevice;
        private static string playermaster;
        private static string globalrekordboxid1, globalrekordboxid2;
        private static int playernumber;
        private static string fadermaster;
        public static string playername;

        private static Tuple<int, string> deck1, deck2;
        
        public static string trackpath, trackpath2;        
        private static string tracktitle1, trackartist1;
        private static string tracktitle2, trackartist2;
        private static string albumartid1, albumartid2;

        private static List<string> currentpcid;
        private int j = 0;
        private static int countrbidoccurance1, countrbidoccurance2;
        
        private readonly ILogger _logger;
        public static bool ControllerFound = false;
        public static string ControllerIP;

        private const string DJ_LINK_PACKET = "5173707431576D4A4F4C";
        private const string MAGIC_NUMBER = "11872349AE";

        private Timer _timer;

        private static List<string> getTrackMetadataSeq =new List<string>();
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
                    _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
                    await CapturePacketsAsync();                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
            }
            Console.WriteLine($"StatusCheckerService background task is stopping.");
        }

        private void DoWork(object state)
        {
            OverlayChangeObserver overlayObserver = new OverlayChangeObserver();
            overlayObserver.MixStatusChanged += MixStatusChanged;
            overlayObserver.Start();
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
                    device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrivalUdp);
                    device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrivalTcp);

                    // Open the device for capturing
                    int readTimeoutMilliseconds = 500;

                    if (device is LibPcapLiveDevice)
                    {
                        var livePcapDevice = device as LibPcapLiveDevice;
                        livePcapDevice.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);
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
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrivalUdpInitial);

            // Open the device for capturing
            int readTimeoutMilliseconds = 500;
           if (device is LibPcapLiveDevice)
            {
                var livePcapDevice = device as LibPcapLiveDevice;
                livePcapDevice.Open(DeviceModes.Promiscuous, readTimeoutMilliseconds);
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
        private void device_OnPacketArrivalUdpInitial(object sender, PacketCapture e)
        {
            currentpcid = GetLocalIPAddress();
            RawCapture _packet = e.GetPacket();
            var packet = Packet.ParsePacket(_packet.LinkLayerType, _packet.Data);
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
                        var res = (from ip in currentpcid
                                   where ip.Contains(srcIp.ToString())
                                   select ip).FirstOrDefault();

                        if (srcIp.ToString() != res)
                        {
                            ControllerFound = true;
                            ControllerIP = srcIp.ToString();
                        }
                    }
                }
            }
        }
        private void device_OnPacketArrivalTcp(object sender, PacketCapture e)
        {
            try
            {               
                RawCapture pack = e.GetPacket();
                var packet = Packet.ParsePacket(pack.LinkLayerType, pack.Data);

                var tcpPacket = packet.Extract<TcpPacket>();

                if (tcpPacket != null)
                {
                    IPPacket ipPacket = (IPPacket)tcpPacket.ParentPacket;
                    byte[] magicnumberPacket = ipPacket.PayloadPacket.PayloadDataSegment.Bytes.ToArray();
                    string result = BitConverter.ToString(magicnumberPacket).Replace("-", string.Empty).Substring(108);
                    IPAddress srcIp = ipPacket.SourceAddress;
                    

                    if (result.Contains(MAGIC_NUMBER))
                    {
                        GetPlayerNumberAndRekordBoxId(magicnumberPacket, result); 
                        Players(result);
                    }
                }              
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Console.WriteLine(ex.Message);
            }            
        }
        
        private void Players(string result)
        {
            if (result.StartsWith(MAGIC_NUMBER) & result[20..].StartsWith("1041010F0C140000000C060606020602060606060606") 
                    & result.Contains("1041010F0C140000000C060606020602060606060606") & result.Contains("1042010F001400000000"))
            {
                getTrackMetadataSeq.Add(result);
            }

            if (result.StartsWith(MAGIC_NUMBER) & result[20..].StartsWith("1041010F0C140000000C060606020602060606060606") 
                & result.Contains("1041010F0C140000000C060606020602060606060606") & !result.Contains("1042010F001400000000"))
            {
                getTrackMetadataSeq.Add(result);
            }

            if (!result.StartsWith(MAGIC_NUMBER) & result.Contains("1042010F0014"))
            {
                var t1 = getTrackMetadataSeq.LastOrDefault(result);

                getTrackMetadataSeq.RemoveAt(getTrackMetadataSeq.Count - 1);

                getTrackMetadataSeq.Add(t1 + result);
            }          

            if (getTrackMetadataSeq.Count > 3)
            {
                foreach(var x in getTrackMetadataSeq)
                {
                    Player1(x);
                    Player2(x);
                    
                }
                getTrackMetadataSeq.Clear();
            }
        }

        private void GetPlayerNumberAndRekordBoxId(byte[] magicnumberPacket, string result)
        {
            if (result.Contains("1021020F02140000000C060600000000000000000000"))
            {   
                int mp_len = magicnumberPacket.Length;

                playernumber = Convert.ToInt32(BitConverter.ToString(magicnumberPacket[(mp_len - 9)..(mp_len - 8)]), 16);
                string rbid = result.Substring(result.Length - 8, 8);

                if (playernumber == 11)
                {
                    globalrekordboxid1 = rbid;
                    deck1 = Tuple.Create(playernumber, rbid);
                }

                if (playernumber == 12)
                {
                    globalrekordboxid2 = rbid;
                    deck2 = Tuple.Create(playernumber, rbid);
                }
            }
        }
        private void Player1(string result)
        {
            countrbidoccurance1 = globalrekordboxid1 != null ? Regex.Matches(result, globalrekordboxid1).Count : 0;

            if (countrbidoccurance1 >= 2)
            {
                (tracktitle1, trackartist1, albumartid1) = GetTrackMetaData(result);

                if (tracktitle1 != null | trackartist1 != null)
                {
                    trackpath = tracktitle1 != null & trackartist1 != null
                        ? trackartist1.Remove(trackartist1.Length - 1) + " - " + tracktitle1.Remove(tracktitle1.Length - 1)
                        : tracktitle1 != null & trackartist1 == null ? tracktitle1.Remove(tracktitle1.Length - 1) : "ID - ID";
                }
                if (albumartid1 != null)
                {
                    //TODO
                }
            }
        }

        private void Player2(string result)
        {
            countrbidoccurance2 = globalrekordboxid2 != null ? Regex.Matches(result, globalrekordboxid2).Count : 0;

            if (countrbidoccurance2 >= 2)
            {
                (tracktitle2, trackartist2, albumartid2) = GetTrackMetaData(result);

                if (tracktitle2 != null | trackartist2 != null)
                {
                    trackpath2 = tracktitle2 != null & trackartist2 != null
                    ? trackartist2.Remove(trackartist2.Length - 1) + " - " + tracktitle2.Remove(tracktitle2.Length - 1)
                    : tracktitle2 != null & trackartist2 == null ? tracktitle2.Remove(tracktitle2.Length - 1) : "ID - ID";
                }
                if (albumartid2 !=null)
                {
                    //TODO
                }
            }
        }
        private Tuple<string, string, string> GetTrackMetaData(string result)
        {
            string[] metadataPacket = result.Split(MAGIC_NUMBER + result[10..64]);
            if (metadataPacket.Length >= 13)
            {
                TrackMetaDataModel trackMetaDataModel = new()
                {                   
                    MainID = metadataPacket[1][12..20],
                    TrackTitle = metadataPacket[1][40..(40 + Convert.ToInt32(metadataPacket[1][22..30], 16) * 2)],
                    ArtistName = metadataPacket[2][40..(40 + Convert.ToInt32(metadataPacket[2][22..30], 16) * 2)]
                };

                string title = trackMetaDataModel.TrackTitle;
                string artist = trackMetaDataModel.ArtistName;
                string albumartid = null;

                title = title.Length != 2 ? HextoString(title).Trim() : null;
                artist = artist.Length != 2 ? HextoString(artist).Trim() : null;
                albumartid = null;

                return new Tuple<string, string, string>(title, artist, albumartid);
            }
            if (metadataPacket.Length == 8)
            {
                TrackMetaDataModel trackMetaDataModel = new()
                {
                    FileNamePath = metadataPacket[5][40..(36 + Convert.ToInt32(metadataPacket[5][22..30], 16) * 2)]
                };

                string albumart = TrackMetadataDetails.GetTrackMetaData(HextoString(trackMetaDataModel.FileNamePath));
                return new Tuple<string, string, string>(null, null, albumart);
            }
            return new Tuple<string, string, string>(null, null, null);
        }

        private void device_OnPacketArrivalUdp(object sender, PacketCapture e)
        {
            RawCapture _packet = e.GetPacket();
            var packet = Packet.ParsePacket(_packet.LinkLayerType, _packet.Data);
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

                    if (srcIp.ToString() == ControllerIP & dstPort == 50002 & result.StartsWith(DJ_LINK_PACKET))
                    {
                        playername = Encoding.Default.GetString(magicnumber[11..24]).Replace("\0", string.Empty);
                        var playernumber = Convert.ToInt32(BitConverter.ToString(magicnumber[33..34]), 16);

                        if (result.StartsWith(DJ_LINK_PACKET + "0A"))
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
        public static List<string> GetLocalIPAddress()
        {
            List<string> GetIP = new List<string>();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            GetIP.Add(ip.Address.ToString());
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
            return Encoding.BigEndianUnicode.GetString(bb);
        }

        public void MixStatusChanged(object sender, OverlayChangeObserver.MixStatusChangedEventArgs e)
        {
            if (e.Player1)
            {
                _tracklisthubContext.Clients.All.SendAsync("NowPlaying", trackartist1, tracktitle1);
            }
            if (e.Player2)
            {
                _tracklisthubContext.Clients.All.SendAsync("NowPlaying", trackartist2, tracktitle2);
            }
        }
    }
}
