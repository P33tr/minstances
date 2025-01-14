﻿"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/statusHub").build();
connection.start();
connection.on("ReceiveMessage", function ( message) {
    var li = document.createElement("li");
    document.getElementById("messagesList").appendChild(li);
    // We can assign user-supplied strings to an element's textContent because it
    // is not interpreted as markup. If you're assigning in any other way, you 
    // should be aware of possible script injection concerns.
    li.textContent = `${message}`;
});