using modzero.Logger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace modzero.Umleitung
{
    public partial class PromptPreferences : Form
    {
        Boolean m_debug_checked;
        int m_debug_level;

        Boolean m_use_system_dns;
        String m_ip_primary;
        String m_ip_alternate;

        public PromptPreferences()
        {
            InitializeComponent();

            m_debug_level = Properties.Settings.Default.umlDebug;

            if (m_debug_level > 1)
                m_debug_checked = true;
            else
                m_debug_checked = false;

            this.checkBox_debug.Checked = m_debug_checked;

            m_use_system_dns = !Properties.Settings.Default.useCustomUpstream;

            this.radioSystemDns.Checked = m_use_system_dns;
            this.radioOwnDns.Checked = !m_use_system_dns;

            m_ip_primary = Properties.Settings.Default.upstreamDNS1;
            this.dnsIpPrimary.Text = m_ip_primary;

            m_ip_alternate = Properties.Settings.Default.upstreamDNS2;
            this.dnsIpAlternate.Text = m_ip_alternate;
        }

        public void ShowPrefsDialog()
        {
            DialogResult res;
            res = this.ShowDialog();

            m_ip_primary = this.dnsIpPrimary.Text;
            m_ip_alternate = this.dnsIpAlternate.Text;

            if (res == DialogResult.OK)
            {
                // save settings
                Properties.Settings.Default.umlDebug = (m_debug_checked ? 2 : 1);
                Properties.Settings.Default.useCustomUpstream = !m_use_system_dns;
                Properties.Settings.Default.upstreamDNS1 = m_ip_primary;
                Properties.Settings.Default.upstreamDNS2 = m_ip_alternate;
                Properties.Settings.Default.Save();
            }
            return;
        }

        private void radioSystemDns_CheckedChanged(object sender, EventArgs e)
        {
            m_use_system_dns = (radioSystemDns.Checked ? true : false);
        }

        private void radioOwnDns_CheckedChanged(object sender, EventArgs e)
        {
            m_use_system_dns = (radioOwnDns.Checked ? false : true);
        }

        private void checkBox_debug_CheckedChanged(object sender, EventArgs e)
        {
            m_debug_checked = checkBox_debug.Checked;
        }
    }
}
