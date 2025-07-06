//
//  LunarConsoleHttpClient.cs
//
//  Lunar Unity Mobile Console
//  https://github.com/SpaceMadness/lunar-unity-console
//
//  Copyright 2015-2021 Alex Lementuev, SpaceMadness.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//


﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR_WIN
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
#endif

namespace LunarConsoleEditorInternal
{
    delegate void LunarConsoleHttpDownloaderCallback(string result, Exception error);

    class LunarConsoleHttpClient
    {
        private Uri m_uri;
        private WebClient m_client;

        #if UNITY_EDITOR_WIN

        static LunarConsoleHttpClient()
        {
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
        }

        private static bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                        chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                        bool chainIsValid = chain.Build((X509Certificate2)certificate);
                        if (!chainIsValid)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        #endif // UNITY_EDITOR_WIN

        public LunarConsoleHttpClient(string uri)
            : this(new Uri(uri))
        {
        }

        public LunarConsoleHttpClient(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            m_uri = uri;
            m_client = new WebClient();
        }

        public void UploadData(string data, LunarConsoleHttpDownloaderCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            if (m_client == null)
            {
                throw new InvalidOperationException("Already downloading something");
            }

            m_client.UploadStringCompleted += delegate(object sender, UploadStringCompletedEventArgs e)
            {
                Utils.DispatchOnMainThread(delegate()
                {
                    if (this.IsShowingProgress)
                    {
                        EditorUtility.ClearProgressBar();
                    }

                    if (!e.Cancelled)
                    {
                        callback(e.Result, e.Error);
                    }
                });
            };

            if (this.IsShowingProgress && EditorUtility.DisplayCancelableProgressBar("Lunar Mobile Console", "Connecting...", 1.0f))
            {
                Cancel();
            }

            m_client.UploadStringAsync(m_uri, data);
        }

        public void DownloadString(LunarConsoleHttpDownloaderCallback callback)
        {
            if (callback == null)
            {
                throw new ArgumentNullException("callback");
            }

            if (m_client == null)
            {
                throw new InvalidOperationException("Already downloading something");
            }

            m_client.DownloadStringCompleted += delegate(object sender, DownloadStringCompletedEventArgs e)
            {
                Utils.DispatchOnMainThread(delegate()
                {
                    if (this.IsShowingProgress)
                    {
                        EditorUtility.ClearProgressBar();
                    }

                    if (!e.Cancelled)
                    {
                        callback(e.Result, e.Error);
                    }
                });
            };

            if (this.IsShowingProgress && EditorUtility.DisplayCancelableProgressBar("Lunar Mobile Console", "Connecting...", 1.0f))
            {
                Cancel();
            }

            m_client.DownloadStringAsync(m_uri);
        }

        public void Cancel()
        {
            m_client.CancelAsync();
        }

        private static string ToHumanReadableLength(long bytes, bool addPrefix = true)
        {
            if (bytes < 1024)
            {
                return addPrefix ? string.Format("{0} bytes", bytes) : bytes.ToString();
            }

            if (bytes < 1024 * 1024)
            {
                float kbytes = bytes / 1024.0f;
                return addPrefix ? string.Format("{0} kb", kbytes.ToString("F1")) : kbytes.ToString("F1");
            }

            float mbytes = bytes / 1024.0f / 1024.0f;
            return addPrefix ? string.Format("{0} Mb", mbytes.ToString("F1")) : mbytes.ToString("F1");
        }

        public bool IsShowingProgress { get; set; }
    }
}