/*
 * ------------------------------------------------------------------------------
 *
 * This file is part of: mod0Umleitung - DNS-masquerading server for Windows.
 *
 * ------------------------------------------------------------------------------
 *
 * BSD 3-Clause License
 *
 * Copyright (c) 2017, modzero GmbH
 * Author: Thorsten Schroeder
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 *
 * * Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 *
 * * Neither the name of the copyright holder nor the names of its
 *   contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * * NON-MILITARY-USAGE CLAUSE
 *   Redistribution and use in source and binary form for military use and
 *   military research is not permitted. Infringement of these clauses may
 *   result in publishing the source code of the utilizing applications and
 *   libraries to the public. As this software is developed, tested and
 *   reviewed by *international* volunteers, this clause shall not be refused
 *   due to the matter of *national* security concerns.
 *  
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * ------------------------------------------------------------------------------
*/


using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using modzero.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace modzero.Umleitung.Server
{
    public class UmleitungServer
    {
        public uint DebugLevel;
        private IPAddress m_listen_ip;
        private DNSMasqConfig m_masq_config = null;
        private m0Logger m_log;
        private DnsServer m_server;
        public String config_dir;

        public bool IsRunning { get; private set; }

        private void init_umleitung_server()
        {
            m_log = new m0Logger(m0Logger.m0LogDestination.LOG_NONE);
            this.IsRunning = false;

            config_dir = System.Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData) +"\\modzero\\umleitung_server";

            if (!Directory.Exists(config_dir))
            {
                try
                {
                    Directory.CreateDirectory(config_dir);
                }
                catch (Exception /*e*/)
                {
                    throw;
                }
            }
        }

        public UmleitungServer()
        {
            m_masq_config = new DNSMasqConfig();
            m_masq_config.DNSMasqEntries = new List<DNSMasqHost>();
            m_listen_ip = IPAddress.Any;
            init_umleitung_server();
        }

        public UmleitungServer(IPAddress listen)
        {
            m_masq_config = new DNSMasqConfig();
            m_masq_config.DNSMasqEntries = new List<DNSMasqHost>();
            m_listen_ip = listen;
            init_umleitung_server();
        }

        public UmleitungServer(String path)
        {
            m_listen_ip = IPAddress.Any;
            ReadMasqConfig(path);
            init_umleitung_server();
        }

        public UmleitungServer(String path, IPAddress listen)
        {
            ReadMasqConfig(path);
            m_listen_ip = listen;
            init_umleitung_server();
        }

        public void EnableLogging(m0Logger log)
        {
            m_log = log;
        }

        public void AddMasq(DNSMasqHost h)
        {
            if (h == null)
                return;
            m_masq_config.DNSMasqEntries.Add(h);
        }

        public void AddMasq(String host, String ip4)
        {
            this.AddMasq(host, ip4, null);
        }

        public void AddMasq(String host, String ip4, String ip6)
        {
            DNSMasqHost h = new DNSMasqHost();

            h.name = host;
            h.a = ip4;
            h.aaaa = ip6;
            m_masq_config.DNSMasqEntries.Add(h);
        }

        public void SaveMasq(String filename)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DNSMasqConfig));
                using (StreamWriter sw = new StreamWriter(filename))
                {
                    serializer.Serialize(sw, m_masq_config);
                }
            }
            catch (Exception e)
            {
                m_log.WriteLine("[e] Save Masquerading Rules error: " + e.Message);
                throw;
            }
        }

        public void ReadMasqConfig()
        {
            String filename = "default.conf";

            if (!File.Exists(Path.Combine(config_dir, filename)))
            {
                this.AddMasq("www.contoso.gack", "127.0.0.1", "::1");
                this.AddMasq("contoso.gack", "127.0.0.2");
                this.AddMasq("test.mod0.de", "10.42.1.23");
                this.SaveMasq(Path.Combine(config_dir, filename));
                return;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DNSMasqConfig));
                using (StreamReader sr = new StreamReader(Path.Combine(config_dir, filename)))
                {
                    m_masq_config = (DNSMasqConfig)serializer.Deserialize(sr);
                }
            }
            catch (Exception e)
            {
                m_log.WriteLine("[e] ReadMasqConfig error: " + e.Message);
                throw;
            }
        }

        public void ReadMasqConfig(String filename)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DNSMasqConfig));
                using (StreamReader sr = new StreamReader(filename))
                {
                    m_masq_config = (DNSMasqConfig)serializer.Deserialize(sr);
                }
            }
            catch (Exception e)
            {
                m_log.WriteLine("[e] ReadMasqConfig error: " + e.Message);
                throw;
            }
        }

        public void DumpRunningConfig()
        {
            m_log.WriteLine(String.Format("[*] Dumping {0} active DNS masquerading rules:", m_masq_config.DNSMasqEntries.Count));

            m_masq_config.DNSMasqEntries.ForEach(h => {
                m_log.WriteLine(String.Format("[-] entry {0}: {1} {2}", h.name, h.a, h.aaaa));
            });
        }

        public void RunConsole()
        {
            m_log.WriteLine("[*] mod0 Umleitung - Starting DNS Masquerading Server.");

            using (DnsServer server = new DnsServer(IPAddress.Any, 10, 10))
            {
                server.QueryReceived += OnQueryReceived;

                server.Start();
                this.IsRunning = true;
                m_log.WriteLine("Press any key to stop server");
                Console.ReadLine();
            }
        }

        public void Run()
        {
            m_log.WriteLine("[*] mod0 Umleitung - Starting DNS Masquerading Server.");

            m_server = new DnsServer(IPAddress.Any, 10, 10);

            m_log.WriteLine(2, "[d] Default DNS Upstream Server:");

            DnsClient.GetLocalConfiguredDnsServers().ForEach(srv => {
                m_log.WriteLine(2, "[d]    - " + srv.ToString());
            });

            m_server.QueryReceived += OnQueryReceived;
            m_server.Start();
            this.IsRunning = true;
        }

        public void Stop()
        {
            m_log.WriteLine("[*] mod0 Umleitung - Stopping DNS Masquerading Server.");

            if (this.IsRunning)
                m_server.Stop();

            this.IsRunning = false;
        }

        async Task OnQueryReceived(object sender, QueryReceivedEventArgs eventArgs)
        {
            DnsMessage query = eventArgs.Query as DnsMessage;
            DnsMessage response = null;

            if (query == null)
                return;

            response = await umleitung_process_request(query);

            eventArgs.Response = response;
        }

        private async Task<DnsMessage> umleitung_process_request(DnsMessage query)
        {
            DnsMessage response = query.CreateResponseInstance();
            DomainName queryhost = DomainName.Parse(query.Questions[0].Name.ToString());

            if ((query.Questions.Count == 1))
            {
                m_log.WriteLine("[+] Processing " + query.Questions[0].RecordType + " query for " + queryhost);

                Boolean match = false;
                IPAddress ip4 = null;
                IPAddress ip6 = null;

                // handle masqueraded entries first
                m_masq_config.DNSMasqEntries.ForEach(h =>
                {
                    if (queryhost.ToString().StartsWith(h.name))
                    {
                        match = true;
                        response.ReturnCode = ReturnCode.NoError;

                        if (query.Questions[0].RecordType == RecordType.A)
                        {
                            ip4 = IPAddress.Parse(h.a);
                            ARecord new_a = new ARecord(query.Questions[0].Name, 666, ip4);
                            response.AnswerRecords.Add(new_a);
                        }
                        else if (query.Questions[0].RecordType == RecordType.Aaaa)
                        {
                            ip6 = IPAddress.Parse(h.aaaa);
                            AaaaRecord new_aaaa = new AaaaRecord(query.Questions[0].Name, 666, ip6);
                            response.AnswerRecords.Add(new_aaaa);
                        }
                    }
                });

                if(match)
                    return response;

                // send query to upstream server
                DnsQuestion question = query.Questions[0];

                DnsMessage upstreamResponse = await DnsClient.Default.ResolveAsync(
                    question.Name, question.RecordType, question.RecordClass);

                // TODO: Implement settings form to define own list of upstream DNS servers.
                //DnsClient dnsc = new DnsClient(IPAddress.Parse("10.1.1.2"), 10000);
                //DnsMessage upstreamResponse = await dnsc.ResolveAsync(
                //    question.Name, question.RecordType, question.RecordClass);

                // if we got an answer, copy it to the message sent to the client
                if (upstreamResponse != null && upstreamResponse.AnswerRecords.Count > 0)
                {
                    foreach (DnsRecordBase record in (upstreamResponse.AnswerRecords))
                    {
                        response.AnswerRecords.Add(record);
                    }

                    foreach (DnsRecordBase record in (upstreamResponse.AdditionalRecords))
                    {
                        response.AdditionalRecords.Add(record);
                    }
                }
                else
                {
                    // no dns record for queried host
                    if (upstreamResponse == null)
                    {
                        m_log.WriteLine(2, "upstreamResponse == null (timeout?)");
                    }
                } 
                response.ReturnCode = ReturnCode.NoError;
            }
            return response;
        }
    }
}
