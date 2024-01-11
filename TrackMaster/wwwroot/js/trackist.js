"use strict";
//var _twitchstatus = '';
//var _ethernetstatus = '';

var connection = new signalR.HubConnectionBuilder().withUrl("/trackistHub").withAutomaticReconnect().build();

connection.start().then(function () {}
).catch(function (err) {
    return console.error(err.toString());
});

connection.on("PlayerOne", function (_id, message) {
    switch (_id) {  
        case 1:
            $('#status-player-one').text(message);
            break;
        case 2:
            $('#loadedfrom-player-one').text(message);
            break;
        case 3:
            $('#master-player-one').text(message);
            break;
        case 4:
            $('#fader-player-one').text(message);
            break;
        case 5:
            if (localStorage.getItem("trackname-player-one") == null || localStorage.getItem("trackname-player-one") != message) {
                localStorage.setItem("trackname-player-one", message);
                $('#trackname-player-one').text(message);
            }
            else {
                $('#trackname-player-one').text(localStorage.getItem("trackname-player-one"));
            }            
            break;
        case 6:
            if (localStorage.getItem("artistname-player-one") == null || localStorage.getItem("artistname-player-one") != message) {
                localStorage.setItem("artistname-player-one", message);
                $('#artistname-player-one').text(message);
            }
            else {
                $('#artistname-player-one').text(localStorage.getItem("artistname-player-one"));
            }
            break;
        case 7:
            if (localStorage.getItem("artwork-player-one") == null || localStorage.getItem("artwork-player-one") != message) {
                localStorage.setItem("artwork-player-one", message);
                $('#artwork-player-one').attr("src", message);
            }
            else {
                $('#artwork-player-one').attr("src",localStorage.getItem("artwork-player-one"));
            }
            break;
        case 8:
            if (localStorage.getItem("duration-player-one") == null || localStorage.getItem("duration-player-one") != message) {
                localStorage.setItem("duration-player-one", message);
                $('#duration-player-one').text(message);
            }
            else {
                $('#duration-player-one').text(localStorage.getItem("duration-player-one"));
            }
            break;
        case 9:
            if (localStorage.getItem("key-player-one") == null || localStorage.getItem("key-player-one") != message) {
                localStorage.setItem("key-player-one", message);
                $('#key-player-one').text(message);
            }
            else {
                $('#key-player-one').text(localStorage.getItem("key-player-one"));
            }
            break;
        case 10:
            if (localStorage.getItem("genre-player-one") == null || localStorage.getItem("genre-player-one") != message) {
                localStorage.setItem("genre-player-one", message);
                $('#genre-player-one').text(message);
            }
            else {
                $('#genre-player-one').text(localStorage.getItem("genre-player-one"));
            }
            break;
    }   
});

connection.on("PlayerTwo", function (_id, message) {
    switch (_id) {
        case 1:
            $('#status-player-two').text(message);
            break;
        case 2:
            $('#loadedfrom-player-two').text(message);
            break;
        case 3:
            $('#master-player-two').text(message);
            break;
        case 4:
            $('#fader-player-two').text(message);
            break;
        case 5:
            if (localStorage.getItem("trackname-player-two") == null || localStorage.getItem("trackname-player-two") != message) {
                localStorage.setItem("trackname-player-two", message);
                $('#trackname-player-two').text(message);
            }
            else {
                $('#trackname-player-two').text(localStorage.getItem("trackname-player-two"));
            }
            break;
        case 6:
            if (localStorage.getItem("artistname-player-two") == null || localStorage.getItem("artistname-player-two") != message) {
                localStorage.setItem("artistname-player-two", message);
                $('#artistname-player-two').text(message);
            }
            else {
                $('#artistname-player-two').text(localStorage.getItem("artistname-player-two"));
            }
            break;
        case 7:
            if (localStorage.getItem("artwork-player-two") == null || localStorage.getItem("artwork-player-two") != message) {
                localStorage.setItem("artwork-player-two", message);
                $('#artwork-player-two').attr("src", message);
            }
            else {
                $('#artwork-player-two').attr("src",localStorage.getItem("artwork-player-two"));
            }
            break;
        case 8:
            if (localStorage.getItem("duration-player-two") == null || localStorage.getItem("duration-player-two") != message) {
                localStorage.setItem("duration-player-two", message);
                $('#duration-player-two').text(message);
            }
            else {
                $('#duration-player-two').text(localStorage.getItem("duration-player-two"));
            }
            break;
        case 9:
            if (localStorage.getItem("key-player-two") == null || localStorage.getItem("key-player-two") != message) {
                localStorage.setItem("key-player-two", message);
                $('#key-player-two').text(message);
            }
            else {
                $('#key-player-two').text(localStorage.getItem("key-player-two"));
            }
            break;
        case 10:
            if (localStorage.getItem("genre-player-two") == null || localStorage.getItem("genre-player-two") != message) {
                localStorage.setItem("genre-player-two", message);
                $('#genre-player-two').text(message);
            }
            else {
                $('#genre-player-two').text(localStorage.getItem("genre-player-two"));
            }
            break;
    }   
});

connection.on("DeviceAndTwitchStatus", function (_id, message) {
    console.log(message);
    switch (_id) {
        case 1:
            $("#loading").hide();
            $("#playerinfo").show();
            $('#ethernet-device').text(message);           
            break;
        case 2:            
            $('#twitch-status').text(message);
            break;
        case 3:
            $('#ethernet-device').text(message);  
            $("#loading").show();
            $("#playerinfo").hide();
            break;
        case 4:
            $('#discord-status').text(message);
            console.log(message);
            break;
    }
});

connection.on("NowPlaying", function (artist, track, artwork, showartwork) {
    if (localStorage.getItem("nowplaying_artist") == null || (localStorage.getItem("nowplaying_artist") != artist)) {
        localStorage.setItem("nowplaying_artist", artist);
        $('#artist').text(artist);        
    }
    else {
        $('#artist').text(localStorage.getItem("nowplaying_artist"));
    }
    if (localStorage.getItem("nowplaying_track") == null || (localStorage.getItem("nowplaying_track") != track)) {
        localStorage.setItem("nowplaying_track", track);
        $('#track').text(track);
    }
    else {
        $('#track').text(localStorage.getItem("nowplaying_track"));
    }
    if (localStorage.getItem("nowplaying_artwork") == null || (localStorage.getItem("nowplaying_artwork") != artwork)) {
        localStorage.setItem("nowplaying_artwork", artwork);
        $('#artwork').attr("src", artwork);
    }
    else {
        $('#artwork').text(localStorage.getItem("nowplaying_artwork"));
    }

    if ($("#artwork").attr("src") == '/Images/Cover-no-artwork.jpg') {
        $("#artwork_div").hide();
    }
    else {
        $("#artwork_div").show();
    }

    if ($("#artist").text() == "null") {
        $("#artist").hide();
    }
    else {
        $("#artist").show();
    }

    if (localStorage.getItem("show_artwork") == null || (localStorage.getItem("show_artwork") != showartwork)) {
        localStorage.setItem("show_artwork", showartwork);
    }
    else {
        $('#show_artwork').text(localStorage.getItem("nowplaying_artwork"));
    }

    if (showartwork) {
        $("#artwork_div").show();
    }
    else {
        $("#artwork_div").hide();
    }
});

connection.on("Overlay", function (showartwork) {

    if (localStorage.getItem("show_artwork") == null || (localStorage.getItem("show_artwork") != showartwork)) {
        localStorage.setItem("show_artwork", showartwork);
    }
    else {
        $('#show_artwork').text(localStorage.getItem("nowplaying_artwork"));
    }

    if (showartwork) {
        $("#artwork_div").show();
    }
    else {
        $("#artwork_div").hide();
    }
});

function OverLay() {

    if (localStorage.getItem("show_artwork") == 'true') {
        $("#artwork_div").show();
        console.log('show');
    }
    else {
        $("#artwork_div").hide();
        console.log('hide');
    }
}

function Nowplaying()
{
    if (localStorage.getItem("nowplaying_artist") != null) {
        $('#artist').text(localStorage.getItem("nowplaying_artist"));
        if ($("#artist").text() == "null") {
            $("#artist").hide();
        }
        else {
            $("#artist").show();
        }
    }
    if (localStorage.getItem("nowplaying_track") != null) {
        $('#track').text(localStorage.getItem("nowplaying_track"));
    }
    if (localStorage.getItem("nowplaying_artwork") != null) {
        $('#artwork').attr("src", localStorage.getItem("nowplaying_artwork"));
        if ($("#artwork").attr("src") == '/Images/Cover-no-artwork.jpg') {
            $("#artwork_div").hide();
        }
        else {
            $("#artwork_div").show();
        }
    }
}

function PlayerOneRestoreMetadata() {
    if (localStorage.getItem("trackname-player-one") != null) {
        $('#trackname-player-one').text(localStorage.getItem("trackname-player-one"));
    }
    if (localStorage.getItem("artistname-player-one") != null) {
        $('#artistname-player-one').text(localStorage.getItem("artistname-player-one"));
    }
    if (localStorage.getItem("artwork-player-one") != null) {
        $('#artwork-player-one').attr("src",localStorage.getItem("artwork-player-one"));
    }
    if (localStorage.getItem("duration-player-one") != null) {
        $('#duration-player-one').text(localStorage.getItem("duration-player-one"));
    }
    if (localStorage.getItem("key-player-one") != null) {
        $('#key-player-one').text(localStorage.getItem("key-player-one"));
    }
    if (localStorage.getItem("genre-player-one") != null) {
        $('#genre-player-one').text(localStorage.getItem("genre-player-one"));
    }
}

function PlayerTwoRestoreMetadata() {
    if (localStorage.getItem("trackname-player-two") != null) {
        $('#trackname-player-two').text(localStorage.getItem("trackname-player-two"));
    }
    if (localStorage.getItem("artistname-player-two") != null) {
        $('#artistname-player-two').text(localStorage.getItem("artistname-player-two"));
    }
    if (localStorage.getItem("artwork-player-two") != null) {
        $('#artwork-player-two').attr("src",localStorage.getItem("artwork-player-two"));
    }   
    if (localStorage.getItem("duration-player-two") != null) {
        $('#duration-player-two').text(localStorage.getItem("duration-player-two"));
    }
    if (localStorage.getItem("key-player-two") != null) {
        $('#key-player-two').text(localStorage.getItem("key-player-two"));
    }
    if (localStorage.getItem("genre-player-two") != null) {
        $('#genre-player-two').text(localStorage.getItem("genre-player-two"));
    }
}