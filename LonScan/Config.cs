using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LonScan
{
    public class Config
    {
        public static string ConfigFile = "LonScan.cfg";

        public string RemoteAddress = "192.168.1.255";
        public int RemoteReceivePort = 3333;
        public int RemoteSendPort = 3334;
        public int SourceSubnet = 1;
        public int SourceNode = 126;
        public int PacketRetries = 3;
        public int PacketTimeout = 1000;
        public int PacketDelay = 100;
        public int LatencyCheckTime = 500;

        public List<string> PacketForgeTemplates = new List<string>();

        public LonDeviceConfig[] DeviceConfigs = new LonDeviceConfig[]
        {
            /*
            new LonDeviceConfig()
            {
                Name = "WVF",
                Addresses = new []{ 2 },
                NvInfos = new NvInfo[]
                {
                    new NvInfo("nviRequest         ", "                 ", "SNVT_obj_request"),
                    new NvInfo("nvoStatus          ", "                 ", "SNVT_obj_status"),
                    new NvInfo("nviTimeSet         ", "nvi_Systemzeit   ", "SNVT_time_stamp"),
                    new NvInfo("nvoFileDirectory   ", "                 ", "SNVT_address"),
                    new NvInfo("nvoTimeSet         ", "Lokale Zeit      ", "SNVT_time_stamp"),
                    new NvInfo("nciLocation        ", "Bezeichnung      ", "SNVT_str_asc"),
                    new NvInfo("N35_m              ", "                 ", ""),
                    new NvInfo("N35_nviDigOut      ", "                 ", "SNVT_state"),
                    new NvInfo("N35_nvoAdRaw       ", "                 ", ""),
                    new NvInfo("WET_nviTsoll[0]    ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WET_nviTsoll[1]    ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WET_m              ", "                 ", ""),
                    new NvInfo("WVF_nviTK          ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviMode        ", "                 ", "SNVT_hvac_mode"),
                    new NvInfo("WVF_nviTEsoll      ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviStaFmf      ", "                 ", ""),
                    new NvInfo("WVF_nviTRG         ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviFBflag      ", "                 ", "SNVT_switch"),
                    new NvInfo("WVF_nviTPO         ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviTPU         ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviTKext       ", "                 ", "SNVT_temp_p"),
                    new NvInfo("nvoWE_ModeGp1      ", "                 ", "SNVT_hvac_mode"),
                    new NvInfo("nvoWE_ModeGp2      ", "                 ", "SNVT_hvac_mode"),
                    new NvInfo("WVF_nvoTsollFa     ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nvoPKsoll      ", "                 ", "SNVT_lev_cont"),
                    new NvInfo("WVF_nvoEngyHldHk   ", "                 ", "SNVT_lev_percent"),
                    new NvInfo("WVF_nvoEngyHldBw   ", "                 ", "SNVT_lev_percent"),
                    new NvInfo("WVF_nvoTPO         ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nvoTPU         ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviTiStaWe     ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nviFBKStart    ", "                 ", "SNVT_lev_disc"),
                    new NvInfo("WVF_nviTPOmin      ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviTiStaWp     ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nviTiAufhEnd   ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nviTiStaUsa    ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nviTiUlvPos    ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nviTiBlockWp   ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nviTiSperr     ", "                 ", "SNVT_time_sec"),
                    new NvInfo("WVF_nvoStaWe       ", "                 ", ""),
                    new NvInfo("WVF_nvoModOgWe     ", "                 ", "SNVT_hvac_mode"),
                    new NvInfo("WVF_nvoStaWp       ", "                 ", ""),
                    new NvInfo("WVF_nvoStaUsa      ", "                 ", ""),
                    new NvInfo("WVF_nvoValve       ", "                 ", "SNVT_lev_percent"),
                    new NvInfo("WVF_nvoStaKtr      ", "                 ", ""),
                    new NvInfo("WVF_nvoPump        ", "                 ", "SNVT_lev_cont"),
                    new NvInfo("WVF_nvoMode        ", "                 ", "SNVT_hvac_mode"),
                    new NvInfo("SYS_pack           ", "                 ", "")
                }
            },*/
            new LonDeviceConfig()
            {
                Name = "UML C1",
                Addresses = new []{ 10, 11 },
                ProgramId = "90010010010A0588",
                NvInfos = new NvInfo[]
                {
                    new NvInfo("nviRequest         ", "                 ", "SNVT_obj_request"),
                    new NvInfo("nvoStatus          ", "                 ", "SNVT_obj_status"),
                    new NvInfo("nviTimeSet         ", "nvi_Systemzeit   ", "SNVT_time_stamp"),
                    new NvInfo("nvoFileDirectory   ", "                 ", "SNVT_address"),
                    new NvInfo("nvoTimeSet         ", "Lokale Zeit      ", "SNVT_time_stamp"),
                    new NvInfo("nviError           ", "nvi_Störcode     ", "SNVT_count"),
                    new NvInfo("nvoError           ", "nvo_Störcode     ", "SNVT_count"),
                    new NvInfo("nciLocation        ", "Bezeichnung      ", "SNVT_str_asc"),
                    new NvInfo("N35_m              ", "                 ", ""),
                    new NvInfo("N35_nviDigOut      ", "                 ", "SNVT_state"),
                    new NvInfo("N35_nvoDiRaw       ", "                 ", ""),
                    new NvInfo("N35_nvoAdRaw       ", "                 ", ""),
                    new NvInfo("N35_nvoBws         ", "                 ", ""),
                    new NvInfo("nviWvEnergyHold    ", "nvi_EnergyHold_HK", "SNVT_lev_percent"),
                    new NvInfo("nviBwEnergyHold    ", "nvi_EnergyHold_BK", "SNVT_lev_percent"),
                    new NvInfo("nviWE_ModeGp1      ", "nvi_WE_Mode_Gr1  ", "SNVT_hvac_mode"),
                    new NvInfo("EH_m               ", "                 ", ""),
                    new NvInfo("WET_nviTsoll[0]    ", "WET_T_Soll_HK    ", "SNVT_temp_p"),
                    new NvInfo("WET_nviTsoll[1]    ", "WET_T_Soll_WW    ", "SNVT_temp_p"),
                    new NvInfo("WET_nvoTsoll[0]    ", "WET_T_Soll_HK    ", "SNVT_temp_p"),
                    new NvInfo("WET_nvoTsoll[1]    ", "WET_T_Soll_WW    ", "SNVT_temp_p"),
                    new NvInfo("WET_nviTist        ", "nvi_WE_T_Kessel  ", "SNVT_temp_p"),
                    new NvInfo("WET_nvoTist        ", "nvo_WET_T_Kessel ", "SNVT_temp_p"),
                    new NvInfo("WET_m              ", "                 ", ""),
                    new NvInfo("nviTaFb            ", "nvi_T_Aussen     ", "SNVT_temp_p"),
                    new NvInfo("nvoTa              ", "T_Aussen         ", "SNVT_temp_p"),
                    new NvInfo("TA_m               ", "                 ", ""),
                    new NvInfo("LX_nviTist[0]      ", "L_T_Boiler       ", "SNVT_temp_p"),
                    new NvInfo("LX_nviTsoll[0]     ", "L_T_Boiler1_Soll ", "SNVT_temp_p"),
                    new NvInfo("LX_nvoPump[0]      ", "L_Pumpe_BK1      ", "SNVT_lev_cont"),
                    new NvInfo("LX_nvoValve[0]     ", "L_Ladeventil_BK1 ", "SNVT_lev_cont"),
                    new NvInfo("LX_m[0]            ", "                 ", ""),
                    new NvInfo("LX_nvoSetPt        ", "                 ", "SNVT_temp_p"),
                    new NvInfo("WVF_nviTPO         ", "T_PufferOben TPO ", "SNVT_temp_p"),
                    new NvInfo("M_nviTVsoll        ", "T_Vorlauf_Soll   ", "SNVT_temp_p"),
                    new NvInfo("M_nviTVist         ", "T_Vorlauf        ", "SNVT_temp_p"),
                    new NvInfo("M_nvoPump          ", "Pumpe            ", "SNVT_lev_cont"),
                    new NvInfo("M_nvoValve         ", "M_Mischventil    ", "SNVT_lev_percent"),
                    new NvInfo("M_m                ", "                 ", ""),
                    new NvInfo("RC_nviMode[0]      ", "                 ", ""),
                    new NvInfo("RC_nvoMode[0]      ", "BDM1_Betriebsart ", ""),
                    new NvInfo("RC_nviFlags[0]     ", "                 ", ""),
                    new NvInfo("RC_nvoFlags[0]     ", "                 ", ""),
                    new NvInfo("RC_nviError[0]     ", "BDM_Störcode     ", "SNVT_count"),
                    new NvInfo("RC_nvoSpaceTemp[0] ", "BDM_T_Raum       ", "SNVT_temp_p"),
                    new NvInfo("RC_nvoHeatSetPt[0] ", "                 ", "SNVT_temp_p"),
                    new NvInfo("RC_nvoBoilSetPt[0] ", "BDM_T_Boiler_Soll", "SNVT_temp_p"),
                    new NvInfo("RC_nviSolarTemp    ", "BDM_TSA_Koll.Aust", "SNVT_temp_p"),
                    new NvInfo("RC_nviSolarEnrgy   ", "BDM_Solargewinn  ", "SNVT_elec_kwh"),
                    new NvInfo("RC_nviSoDayEnrgy   ", "                 ", "SNVT_elec_kwh"),
                    new NvInfo("FA_m               ", "                 ", ""),
                    new NvInfo("FA_nviTVsoll       ", "FA_T_Kessel_Soll ", "SNVT_temp_p"),
                    new NvInfo("FA_nvoTk           ", "FA_T_Kessel_VL   ", "SNVT_temp_p"),
                    new NvInfo("FA_nvoTr           ", "FA_T_Kessel_RL   ", "SNVT_temp_p"),
                    new NvInfo("FA_nviExtTsoll     ", "                 ", "SNVT_temp_p"),
                    new NvInfo("SYS_pack           ", "                 ", ""),
                }
            }/*,
            new LonDeviceConfig()
            {
                // cat PMX_A3_V270.XIF | grep VAR -A3 | tr '\r' ';' | tr '\n' ' ' | sed "s/--/\n/g;" | sed "s/^ * //g" | cut -d ' ' -f 3,2,21
                Name = "PMX150",
                Addresses = new []{ 60 },
                NvInfos = new NvInfo[]
                {
                    new NvInfo("nviRequest       ", "                 ", "SNVT_obj_request"),
                    new NvInfo("nvoStatus        ", "                 ", "SNVT_obj_status"),
                    new NvInfo("nviTimeSet       ", "nvi_Systemzeit   ", "SNVT_time_stamp"),
                    new NvInfo("nvoFileDirectory ", "                 ", "SNVT_address"),
                    new NvInfo("nvoTime          ", "Lokale Zeit      ", "SNVT_time_stamp"),
                    new NvInfo("nviObjEvent      ", "                 ", ""),
                    new NvInfo("nvoObjStatus     ", "                 ", ""),
                    new NvInfo("nvoError         ", "                 ", "SNVT_count"),
                    new NvInfo("nvoWvEnergyHold  ", "nvo_EnergyHold_HK", "SNVT_lev_percent"),
                    new NvInfo("nvoBwEnergyHold  ", "nvo_EnergyHold_BK", "SNVT_lev_percent"),
                    new NvInfo("nviWE_ModeGp1    ", "nvi_WE_Mode_Gr1  ", "SNVT_hvac_mode"),
                    new NvInfo("nviWE_ModeGp3    ", "nvi_WE_Mode_Gr3  ", "SNVT_hvac_mode"),
                    new NvInfo("EH_m             ", "                 ", ""),
                    new NvInfo("WET_nviTsoll[0]  ", "WET_T_Soll_HK    ", "SNVT_temp_p"),
                    new NvInfo("WET_nviTsoll[1]  ", "WET_T_Soll_BK    ", "SNVT_temp_p"),
                    new NvInfo("WET_nvoTist      ", "nvo_WET_T_Kessel ", "SNVT_temp_p"),
                    new NvInfo("BD_nviDsp        ", "                 ", "display"),
                    new NvInfo("GB_nviNsoll      ", "                 ", "SNVT_rpm"),
                    new NvInfo("GB_nvoNist       ", "A_GB_Drehzahl    ", "SNVT_rpm"),
                    new NvInfo("GB_nvoNsoll      ", "A_GB_Drehz. Soll ", "SNVT_rpm"),
                    new NvInfo("GB_m             ", "                 ", ""),
                    new NvInfo("FS_nviMsoll      ", "A_FS_Menge_Soll  ", "SNVT_mass_kilo"),
                    new NvInfo("FS_nvoMotor      ", "                 ", "SNVT_lev_disc"),
                    new NvInfo("FS_nvoTein       ", "                 ", "SNVT_time_sec"),
                    new NvInfo("FS_nvoTaus       ", "                 ", "SNVT_time_sec"),
                    new NvInfo("FS_nvoMpts       ", "                 ", "SNVT_mass_kilo"),
                    new NvInfo("FS_m             ", "                 ", ""),
                    new NvInfo("FS_nviMfoerder   ", "A_FS_Menge_berech", "SNVT_mass_kilo"),
                    new NvInfo("FS_nvoAvgTb_Tk   ", "                 ", "SNVT_temp"),
                    new NvInfo("ZG_nviOn         ", "                 ", "SNVT_lev_disc"),
                    new NvInfo("ZG_nvoHeat       ", "                 ", "SNVT_lev_disc"),
                    new NvInfo("ZG_nvoVent       ", "                 ", "SNVT_lev_disc"),
                    new NvInfo("RO_nviCount      ", "                 ", "SNVT_count"),
                    new NvInfo("RO_nvoCount      ", "                 ", "SNVT_count"),
                    new NvInfo("RO_m             ", "                 ", ""),
                    new NvInfo("PMX_nviLstg      ", "P_Leistung_Soll  ", "SNVT_lev_cont"),
                    new NvInfo("FMF_nvoStatus    ", "                 ", ""),
                    new NvInfo("PMX_nvoLstg      ", "P_Leistung       ", "SNVT_lev_cont"),
                    new NvInfo("PMX_m            ", "                 ", ""),
                    new NvInfo("PMX_nvoToPzs     ", "                 ", "SNVT_switch"),
                    new NvInfo("PMX_nviFromPzs   ", "                 ", "SNVT_switch"),
                    new NvInfo("PMX_nviPzsStatus ", "                 ", "SNVT_obj_status"),
                    new NvInfo("PMX_eeBetrStd    ", "B_Betriebsstunden", "SNVT_time_hour"),
                    new NvInfo("PMX_eeNbrAnhz    ", "B_Anheizvorgänge ", "SNVT_count"),
                    new NvInfo("EXT_m            ", "B_AAT_HFR        ", ""),
                    new NvInfo("EXT_nvoTacho     ", "                 ", "SNVT_time_min"),
                    new NvInfo("EXT_nvoTacho1    ", "                 ", "SNVT_time_min"),
                    new NvInfo("NIC_nvoValue     ", "T_Brennraum      ", "SNVT_temp"),
                    new NvInfo("NIC_nvoTboard    ", "T_Schaltfeld     ", "SNVT_temp_p"),
                    new NvInfo("NIC_nvoAvgVal    ", "T_Brennr._Mittel ", "SNVT_temp"),
                    new NvInfo("TK_nviSetP       ", "T_Kessel_Soll    ", "SNVT_temp_p"),
                    new NvInfo("TK_nvoPID        ", "                 ", "SNVT_lev_percent"),
                    new NvInfo("TK_nvoTemp       ", "T_Kessel         ", "SNVT_temp_p"),
                    new NvInfo("TK_nvoRist       ", "T_Kessel_Rücklauf", "SNVT_temp_p"),
                    new NvInfo("RG_nviSetP       ", "T_Abgas_Soll     ", "SNVT_temp"),
                    new NvInfo("RG_nvoPID        ", "                 ", "SNVT_lev_percent"),
                    new NvInfo("RG_nvoTemp       ", "T_Abgas          ", "SNVT_temp"),
                    new NvInfo("TVB_nvoValue     ", "                 ", "SNVT_temp"),
                    new NvInfo("HW_nviDigInput   ", "                 ", ""),
                    new NvInfo("HW_nviDigOutput  ", "                 ", "")
                }
            }*/
        };

        internal void AddDeviceConfig(LonDeviceConfig xif)
        {
            List<LonDeviceConfig> cfg = DeviceConfigs.ToList();
            cfg.Add(xif);
            DeviceConfigs = cfg.ToArray();
        }

        public static Config Load()
        {
            try
            {
                string json = File.ReadAllText(ConfigFile);
                Config cfg = JsonConvert.DeserializeObject<Config>(json);

                return cfg;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception)
            {
            }
        }
    }
}
