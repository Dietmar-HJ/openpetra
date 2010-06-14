//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2010 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//
using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using Ict.Common.IO;

namespace Ict.Petra.Shared.RemotingSinks.Encryption
{
    /// <summary>
    /// This sink adds encryption to the channel
    /// </summary>
    public class EncryptionServerSink : BaseChannelSinkWithProperties, IServerChannelSink
    {
        private byte[] FEncryptionKey = null;
        private RSAParameters FPrivateKey;
        private IServerChannelSink FNextSink;

        /// <summary>
        /// constructor
        /// </summary>
        public EncryptionServerSink(IServerChannelSink ANextSink, RSAParameters APrivateKey)
        {
            FPrivateKey = APrivateKey;
            FNextSink = ANextSink;
        }

        /// <summary>
        /// required to implement the interface
        /// </summary>
        public IServerChannelSink NextChannelSink
        {
            get
            {
                return FNextSink;
            }
        }

        /// <summary>
        /// Requests processing from the current sink of the response from a method call sent asynchronously.
        /// </summary>
        public void AsyncProcessResponse(IServerResponseChannelSinkStack sinkStack,
            object state, IMessage msg, ITransportHeaders headers, Stream stream)
        {
            // process response
            ProcessResponse(msg, headers, ref stream, state);

            // forward to the next
            sinkStack.AsyncProcessResponse(msg, headers, stream);
        }

        /// <summary>
        /// Returns the Stream onto which the provided response message is to be serialized.
        /// </summary>
        public Stream GetResponseStream(IServerResponseChannelSinkStack sinkStack,
            object state,
            IMessage msg,
            ITransportHeaders headers)
        {
            return null;
        }

        /// <summary>
        /// Requests message processing from the current sink.
        /// </summary>
        public ServerProcessing ProcessMessage(IServerChannelSinkStack sinkStack,
            IMessage requestMsg, ITransportHeaders requestHeaders, Stream requestStream,
            out IMessage responseMsg, out ITransportHeaders responseHeaders,
            out Stream responseStream)
        {
            // process request
            object state = null;

            ProcessRequest(requestMsg, requestHeaders, ref requestStream, ref state);

            sinkStack.Push(this, state);

            ServerProcessing processing = FNextSink.ProcessMessage(sinkStack,
                requestMsg, requestHeaders, requestStream,
                out responseMsg, out responseHeaders, out responseStream);

            if (processing == ServerProcessing.Complete)
            {
                ProcessResponse(responseMsg, responseHeaders, ref responseStream, state);
            }

            return processing;
        }

        /// <summary>
        /// decrypt the request
        /// </summary>
        protected void ProcessRequest(IMessage message, ITransportHeaders headers, ref Stream stream, ref object state)
        {
            if (headers[EncryptionRijndael.GetEncryptionName()] != null)
            {
                if (headers[EncryptionRijndael.GetEncryptionName() + "KEY"] != null)
                {
                    // read the symmetric key, which has been encrypted with our public key
                    RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                    RSA.ImportParameters(FPrivateKey);
                    FEncryptionKey = RSA.Decrypt(
                        Convert.FromBase64String((String)headers[EncryptionRijndael.GetEncryptionName() + "KEY"]), false);
                }

                byte[] EncryptionIV = Convert.FromBase64String((String)headers[EncryptionRijndael.GetEncryptionName() + "IV"]);
                stream = EncryptionRijndael.Decrypt(FEncryptionKey, stream, EncryptionIV);
                state = true;
            }
        }

        /// <summary>
        /// encrypt the response
        /// </summary>
        protected void ProcessResponse(IMessage message, ITransportHeaders headers, ref Stream stream, object state)
        {
            if (state != null)
            {
                byte[] EncryptionIV;
                stream = EncryptionRijndael.Encrypt(FEncryptionKey, stream, out EncryptionIV);
                headers[EncryptionRijndael.GetEncryptionName()] = "Yes";

                // the initialisation vector is no secret, but we need to generate it for each encryption, and it is needed for decryption
                headers[EncryptionRijndael.GetEncryptionName() + "IV"] = Convert.ToBase64String(EncryptionIV);
            }
        }
    }
}