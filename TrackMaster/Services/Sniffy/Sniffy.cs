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
        private static List<string> getTrackMetadataSeq = new List<string>();
        private bool trackloaded_player1, trackloaded_player2;
        private bool AlbumArtloaded_player1, AlbumArtloaded_player2;
        public static string trackpath, trackpath2;
        public static string tracktitle1, trackartist1;
        public static string tracktitle2, trackartist2;
        public static string albumartid1, albumartid2;
        public static string duration1, duration2;
        public static string key1, key2;
        public static string genre1, genre2;

        private static List<string> currentpcid;
        private int j = 0;
        private static int countrbidoccurance1, countrbidoccurance2;
        
        private readonly ILogger _logger;
        public static bool ControllerFound = false;
        public static string ControllerIP;

        private Timer _timer;
        private List<int> totallist;
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
                    int readTimeoutMilliseconds = 1500;

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
            int readTimeoutMilliseconds = 1500;
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

            await Task.Delay(6000);
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

                    if (dstPort == 50000 & result.StartsWith(Constants.DJ_LINK_PACKET + "06"))
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

                    if (srcIp.ToString() == ControllerIP & dstPort == 50002 & result.StartsWith(Constants.DJ_LINK_PACKET))
                    {
                        playername = Encoding.Default.GetString(magicnumber[11..24]).Replace("\0", string.Empty);
                        var playernumber = Convert.ToInt32(BitConverter.ToString(magicnumber[33..34]), 16);

                        if (result.StartsWith(Constants.DJ_LINK_PACKET + "0A"))
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
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 5, tracktitle1);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 6, trackartist1);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 7, albumartid1);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 8, duration1);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 9, key1);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 10, genre1);

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
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 5, tracktitle2);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 6, trackartist2);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 7, albumartid2);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 8, duration2);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 9, key2);
                            //_tracklisthubContext.Clients.All.SendAsync("PlayerOne", 10, genre2);

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
                    

                    if (result.Contains(Constants.MAGIC_NUMBER))
                    {
                        GetPlayerNumberAndRekordBoxId(magicnumberPacket, result);
                        //GetTotalMenuItems(result.Substring(108)); Not using this for now
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
        private void GetPlayerNumberAndRekordBoxId(byte[] magicnumberPacket, string result)
        {
            if (result.Contains(Constants.DataRequest))
            {
                int mp_len = magicnumberPacket.Length;

                playernumber = Convert.ToInt32(BitConverter.ToString(magicnumberPacket[(mp_len - 9)..(mp_len - 8)]), 16);
                string rbid = result.Substring(result.Length - 8, 8);

                if (playernumber == 11)
                {
                    globalrekordboxid1 = rbid;
                    deck1 = Tuple.Create(playernumber, rbid);
                    trackloaded_player1 = false;
                    AlbumArtloaded_player1 = false;
                }

                if (playernumber == 12)
                {
                    globalrekordboxid2 = rbid;
                    deck2 = Tuple.Create(playernumber, rbid);
                    trackloaded_player2 = false;
                    AlbumArtloaded_player2 = false;
                }
            }
        }
        private void GetTotalMenuItems(string result)
        {
            if (result.Contains("1030000F0614"))
            {
                var totalitems = Convert.ToInt32(result[112..114], 16);

                totallist = new List<int>();

                totallist.Add(totalitems);
            }
        }
        private void Players(string result)
        {
            if (result.StartsWith(Constants.MAGIC_NUMBER) & result.Contains(Constants.MenuItemResponse) & result.Contains(Constants.MenuFooterReponse))
            {
                getTrackMetadataSeq.Add(result);
            }

            if (result.StartsWith(Constants.MAGIC_NUMBER) & result.Contains(Constants.MenuItemResponse) & !result.Contains(Constants.MenuFooterReponse))
            {
                getTrackMetadataSeq.Add(result);
            }

            //This will concat any data thats split into multiple packets
            if (!result.StartsWith(Constants.MAGIC_NUMBER) & result.Contains(Constants.MenuFooterReponse))
            {
                var t1 = getTrackMetadataSeq.LastOrDefault(result);

                if (getTrackMetadataSeq.Count != 0)
                {
                    getTrackMetadataSeq.RemoveAt(getTrackMetadataSeq.Count - 1);

                    getTrackMetadataSeq.Add(t1 + result);
                }
            }

            if (getTrackMetadataSeq.Count > 3)
            {
                foreach (var x in getTrackMetadataSeq)
                {
                    Player1(x);
                    Player2(x);
                }
                getTrackMetadataSeq.Clear();
            }
        }        
        private void Player1(string result)
        {
            countrbidoccurance1 = globalrekordboxid1 != null ? Regex.Matches(result, globalrekordboxid1).Count : 0;

            if(!trackloaded_player1)
            {
                //This will insure the data is for the Track Metadata
                if (countrbidoccurance1 >= 2)
                {
                    var returnMetaData = GetTrackMetaData(result);

                    if (returnMetaData != null)
                    {
                        if (returnMetaData.TrackTitle != null | returnMetaData.ArtistName != null)
                        {
                            tracktitle1 = returnMetaData.TrackTitle;
                            trackartist1 = returnMetaData.ArtistName;
                            duration1 = returnMetaData.Duration;
                            key1 = returnMetaData.Key; genre1 = returnMetaData.Genre;

                            trackpath = tracktitle1 != null & trackartist1 != null
                                ? trackartist1 + " - " + tracktitle1
                                : tracktitle1 != null & trackartist1 == null ? tracktitle1 : "ID - ID";

                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 5, tracktitle1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 6, trackartist1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 8, duration1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 9, key1);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 10, genre1);

                            trackloaded_player1 = true;
                        }
                    }
                }
            }

            if (!AlbumArtloaded_player1)
            {
                //This will insure the data is for the Album art which is the filename path
                if (countrbidoccurance1 >= 1)
                {
                    var returnAlbumArtData = GetAlbumArtData(result);

                    if (returnAlbumArtData != null)
                    {
                        if (returnAlbumArtData.AlbumArt != null)
                        {
                            albumartid1 = returnAlbumArtData.AlbumArt;
                            _tracklisthubContext.Clients.All.SendAsync("PlayerOne", 7, albumartid1);

                            AlbumArtloaded_player1 = true;
                        }
                    }
                }
            }
            
        }
        private void Player2(string result)
        {
            countrbidoccurance2 = globalrekordboxid2 != null ? Regex.Matches(result, globalrekordboxid2).Count : 0;

            if(!trackloaded_player2)
            {
                if (countrbidoccurance2 >= 2)
                {
                    var returnMetaData = GetTrackMetaData(result);

                    if (returnMetaData != null)
                    {
                        if (returnMetaData.TrackTitle != null | returnMetaData.ArtistName != null)
                        {
                            tracktitle2 = returnMetaData.TrackTitle; ;
                            trackartist2 = returnMetaData.ArtistName;
                            duration2 = returnMetaData.Duration;
                            key2 = returnMetaData.Key;
                            genre2 = returnMetaData.Genre;

                            trackpath2 = tracktitle2 != null & trackartist2 != null
                            ? trackartist2 + " - " + tracktitle2
                            : tracktitle2 != null & trackartist2 == null ? tracktitle2 : "ID - ID";

                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 5, tracktitle2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 6, trackartist2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 8, duration2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 9, key2);
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 10, genre2);

                            trackloaded_player2 = true;
                        }
                    }
                }
            }
            if (!AlbumArtloaded_player2)
            {
                //This will insure the data is for the Album art which is the filename path 
                if (countrbidoccurance2 >= 1)
                {
                    var returnAlbumArtData = GetAlbumArtData(result);
                    if (returnAlbumArtData != null)
                    {
                        if (returnAlbumArtData.AlbumArt != null)
                        {
                            albumartid2 = returnAlbumArtData.AlbumArt;
                            _tracklisthubContext.Clients.All.SendAsync("PlayerTwo", 7, albumartid2);

                            AlbumArtloaded_player2 = true;
                        }
                    }
                }
            }               
        }
        private static TrackMetaDataModel GetTrackMetaData(string result)
        {
            TrackMetaDataModel returnMetaData = null;
            string[] metadataPacket = result.Split(Constants.MAGIC_NUMBER)
                                            .Where(x => !string.IsNullOrWhiteSpace(x)) //Removes any null spaces from Array
                                            .Where(x => !x.Contains(Constants.MenuHeader)) //Exclude the Header
                                            .Where(x => !x.Contains(Constants.MenuFooterReponse)) //Exclude the Footer
                                            .ToArray();

            if (metadataPacket != null & metadataPacket.Length >= 13)
            {
                TrackMetaDataModel trackMetaDataModel = new()
                {
                    MainID = metadataPacket[0][66..74],
                    TrackTitle = metadataPacket[0][94..(90 + Convert.ToInt32(metadataPacket[0][76..84], 16) * 2)],
                    ArtistName = metadataPacket[1][94..(90 + Convert.ToInt32(metadataPacket[1][76..84], 16) * 2)],
                    Duration = ConvertToMinsSecs(Convert.ToInt32(metadataPacket[3][66..74], 16)),
                    Key = metadataPacket[6][94..(90 + Convert.ToInt32(metadataPacket[6][76..84], 16) * 2)],
                    Genre = metadataPacket[9][94..(90 + Convert.ToInt32(metadataPacket[9][76..84], 16) * 2)]
                };
                returnMetaData = GetTrackMetaData(trackMetaDataModel);

                return returnMetaData;
            }
            return returnMetaData;
        }
        private static TrackMetaDataModel GetAlbumArtData(string result)
        {
            TrackMetaDataModel returnMetaData = null;
            string[] metadataPacket = result.Split(Constants.MAGIC_NUMBER)
                                             .Where(x => !string.IsNullOrWhiteSpace(x)) //Removes any null spaces from Array
                                             .Where(x => !x.Contains(Constants.MenuHeader)) //Exclude the Header
                                             .Where(x => !x.Contains(Constants.MenuFooterReponse)) //Exclude the Footer
                                             .ToArray();

            if (metadataPacket.Length >= 6 & metadataPacket.Length <= 7)
            {
                TrackMetaDataModel trackMetaDataModel = new()
                {
                    AlbumArt = metadataPacket[4][94..(90 + Convert.ToInt32(metadataPacket[4][76..84], 16) * 2)]
                };

                string albumart = TrackMetadataDetails.GetTrackMetaDataFromFile(HextoString(trackMetaDataModel.AlbumArt));

                if (albumart != null)
                {
                    TrackMetaDataModel trackMetaDataModel2 = new()
                    {
                        AlbumArt = "data:image/png;base64," + albumart
                    };
                    return trackMetaDataModel2;
                }
                else
                {
                    TrackMetaDataModel trackMetaDataModel2 = new()
                    {
                        AlbumArt = "/Images/Cover-no-artwork.jpg"
                    };
                    return trackMetaDataModel2;
                }
            }

            return returnMetaData;
        }
        private static string ConvertToMinsSecs(int totalSeconds)
        {
            int seconds = totalSeconds % 60;
            int minutes = totalSeconds / 60;
            string time = minutes + "m " + seconds+ "s";
            return time;
        }
        private static TrackMetaDataModel GetTrackMetaData(TrackMetaDataModel trackMetaDataModel)
        {
            TrackMetaDataModel _trackMetaData = new()
            {
                MainID = trackMetaDataModel.MainID,
                TrackTitle = trackMetaDataModel.TrackTitle != "" ? HextoString(trackMetaDataModel.TrackTitle).Trim() : null,
                ArtistName = trackMetaDataModel.ArtistName != "" ? HextoString(trackMetaDataModel.ArtistName).Trim() : null,
                Genre = trackMetaDataModel.Genre != "" ? HextoString(trackMetaDataModel.Genre).Trim() : null,
                Duration = trackMetaDataModel.Duration != "" ? trackMetaDataModel.Duration : null,
                Key = trackMetaDataModel.Key != "" ? HextoString(trackMetaDataModel.Key).Trim() : null
            }; 

            return _trackMetaData;
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
        private static string HextoString(string InputText)
        {
            byte[] bb = Enumerable.Range(0, InputText.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(InputText.Substring(x, 2), 16))
                             .ToArray();
            return Encoding.BigEndianUnicode.GetString(bb);
        }
        private void MixStatusChanged(object sender, OverlayChangeObserver.MixStatusChangedEventArgs e)
        {
            if (e.Player1)
            {
                _tracklisthubContext.Clients.All.SendAsync("NowPlaying", trackartist1, tracktitle1, albumartid1);
            }
            if (e.Player2)
            {
                _tracklisthubContext.Clients.All.SendAsync("NowPlaying", trackartist2, tracktitle2, albumartid2);
            }
        }
    }
}
