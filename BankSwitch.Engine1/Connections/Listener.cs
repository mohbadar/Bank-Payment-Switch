﻿using BankSwitch.Core.Entities;
using BankSwitch.Engine.ProcessorMangement;
using BankSwitch.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trx.Messaging;
using Trx.Messaging.Channels;
using Trx.Messaging.FlowControl;
using Trx.Messaging.Iso8583;

namespace BankSwitch.Engine.Connections
{
   public class Listener
    {
        //TO INSTANTIATE LISTENER PEER
       TransactionManager trxnManager = new TransactionManager();
        public void StartListener(SourceNode theSourceNode)
        {
            string ipAddress = theSourceNode.IPAddress;
            int port = Convert.ToInt32(theSourceNode.Port);

            TcpListener tcpListener = new TcpListener(port);
            tcpListener.LocalInterface = ipAddress;
            tcpListener.Start();

            ListenerPeer listener = new ListenerPeer(theSourceNode.Id.ToString(), new TwoBytesNboHeaderChannel
                        (new Iso8583Ascii1987BinaryBitmapMessageFormatter(), ipAddress, port),
                         new BasicMessagesIdentifier(11, 41), tcpListener);

            trxnManager.Log("Source: " + theSourceNode.Name + " listening at " + ipAddress + " on " + port);


            listener.Connected += new PeerConnectedEventHandler(listenerPeerConnected);
            listener.Receive += new PeerReceiveEventHandler(Listener_Receive);
            listener.Disconnected += new PeerDisconnectedEventHandler(listenerPeerDisconnected);

        }

        private void listenerPeerDisconnected(object sender, EventArgs e)
        {
            ListenerPeer peer = sender as ListenerPeer;
            if (peer == null) return;
            trxnManager.Log("(Event)Source server disconnected from: " + peer.Name);
            SourceNode theSourceNode = new SourceNodeManager().GetByID(Convert.ToInt32(peer.Name));
            StartListener(theSourceNode);
        }

        private void Listener_Receive(object sender, ReceiveEventArgs e)
        {
            //Cast event sender as ClientPeer
            ListenerPeer sourcePeer = sender as ListenerPeer;

            trxnManager.Log("Listener Peer is now receiving..." + DateTime.Now.ToString("dd/MMM/yyyy hh:mm:ss tt") + Environment.NewLine);

            //Get the Message received
            Iso8583Message incomingMsg = e.Message as Iso8583Message;

            if (incomingMsg == null) return;
   
            long sourceID = Convert.ToInt64(sourcePeer.Name);   //where message is coming from


            Iso8583Message receivedMessage = new TransactionManager().ValidateMessage(incomingMsg, Convert.ToInt32( sourceID));

            sourcePeer.Send(receivedMessage);

            sourcePeer.Close();
            sourcePeer.Dispose();
        }

        private void listenerPeerConnected(object sender, EventArgs e)
        {

            ListenerPeer peer = sender as ListenerPeer;
            trxnManager.Log("Connected to ==> " + peer.Name);
            if (peer == null) return;
        }

    }
}