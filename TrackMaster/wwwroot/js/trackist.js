"use strict";
//var _twitchstatus = '';
//var _ethernetstatus = '';

var connection = new signalR.HubConnectionBuilder().withUrl("/trackistHub").build();

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
            $('#trackmeta-player-one').text(message);
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
            $('#trackmeta-player-two').text(message);
            break;
    }   
});

connection.on("DeviceAndTwitchStatus", function (_id, message) {
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
    }
});