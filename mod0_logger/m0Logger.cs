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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace modzero.Logger
{
    public class m0Logger
    {
        public enum m0LogDestination { LOG_EVENTLOG, LOG_CONSOLE, LOG_FILE, LOG_TEXTBOX, LOG_NONE };

        private System.Diagnostics.EventLog m_eventLog;
        private m0LogDestination m_logdest;
        private String m_logfile;
        private int m_debuglevel;
        private TextBox m_textbox;

        public m0Logger()
        {
            m_logdest = m0LogDestination.LOG_CONSOLE;
            m_debuglevel = 0;
        }

        public m0Logger(m0LogDestination dest) {

            m_logdest = dest;
            m_debuglevel = 0;

            if (m_logdest == m0LogDestination.LOG_EVENTLOG)
            {
                try
                {
                    m_eventLog = new System.Diagnostics.EventLog();

                    if (!System.Diagnostics.EventLog.SourceExists("modzero"))
                    {
                        System.Diagnostics.EventLog.CreateEventSource(
                            "modzero", "Application");
                    }
                    m_eventLog.Source = "modzero";
                    m_eventLog.Log = "Application";
                }
                catch (Exception e)
                {
                    m_logdest = m0LogDestination.LOG_CONSOLE;
                    Console.WriteLine("[e] m0Logger: failed to initialize eventlog: " + e.Message);
                }
            }
            else if (m_logdest == m0LogDestination.LOG_FILE)
            {
                init_filelogging("modzero_debug.log");
            }
            else
            {
            }
        }

        private void init_filelogging(String filename)
        {
            String logdir = System.Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData) + "\\modzero\\debug";

            m_logfile = Path.Combine(logdir, filename);

            try
            {
                if (!Directory.Exists(logdir))
                    Directory.CreateDirectory(logdir);

                m_logdest = m0LogDestination.LOG_FILE;
            }
            catch (Exception e)
            {
                Console.WriteLine("[e] failed to create logdir. falling back to Console logging: " + e.Message);
                m_logdest = m0LogDestination.LOG_CONSOLE;
            }
        }

        public m0Logger(m0LogDestination dest, String filename)
        {
            m_logdest = dest;
            m_debuglevel = 0;
            init_filelogging(filename);
        }

        public m0Logger(m0LogDestination dest, TextBox tb)
        {
            m_logdest = dest;
            m_debuglevel = 1;
            m_textbox = tb;
        }

        public void SetDebuglevel(int l)
        {
            m_debuglevel = l;
        }

        public int GetDebuglevel()
        {
            return m_debuglevel;
        }


        public void WriteLine(String s) {
            if ((m_debuglevel < 0) || (m0LogDestination.LOG_NONE == m_logdest))
                return;

            if (m_logdest == m0LogDestination.LOG_CONSOLE)
            {
                Console.WriteLine(s);
            }
            else if (m_logdest == m0LogDestination.LOG_EVENTLOG)
            {
                m_eventLog.WriteEntry(s);
            }
            else if (m_logdest == m0LogDestination.LOG_FILE)
            {
                append_log(s);
            }
            else if (m_logdest == m0LogDestination.LOG_TEXTBOX)
            {
                try
                {
                    m_textbox.AppendText(s + "\r\n");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Writing log to Textbox failed: " + e.Message, "Exception");
                    throw;
                }
            }
        }

        public void WriteLine(int level, String s)
        {
            if ((m_debuglevel < level) || (m0LogDestination.LOG_NONE == m_logdest))
                return;

            this.WriteLine(s);
        }

        private void append_log(String msg)
        {
            StreamWriter sw = null;

            try
            {
                sw = File.AppendText(m_logfile);
                sw.WriteLine(msg);
                sw.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine("[e] failed to write to logfile. falling back to Console logging: " + e.Message);
                m_logdest = m0LogDestination.LOG_CONSOLE;
            }
            finally
            {
                if(sw != null)
                    sw.Close();
            }
        }

    }
}
