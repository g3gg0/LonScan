using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LonScan
{
    public class LonStandardTypes
    {
        public class LonType
        {
            public string Name { get; set; }
            public int Id { get; set; }

            public LonType(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }


        public static List<LonType> Types { get; set; } = new List<LonType>();

        internal static LonType Get(int id)
        {
            return Types.Where(t => t.Id == id).FirstOrDefault();
        }

        internal static LonType Get(string format)
        {
            return Types.Where(t => t.Name == format).FirstOrDefault();
        }

        /* old-style - has to get converted to let the LonType do the work */
        internal static string ToString(byte[] data, int offset, int length, LonType type)
        {
            string value = "";

            if (type == null)
            {
                return "";
            }

            switch (type.Name)
            {
                case "display":
                    {
                        value = "" + (char)data[offset + 0] + (char)data[offset + 1] + " " + data[offset + 2].ToString();
                        break;
                    }

                case "SNVT_count":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        value = "" + raw;
                        break;
                    }

                case "SNVT_power":
                    {
                        decimal raw = GetUnsigned(data, offset, 2) / 10;

                        value = "Power: " + raw;
                        break;
                    }

                case "SNVT_rpm":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        value = "RPM: " + raw;
                        break;
                    }

                case "SNVT_time_hour":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        value = "" + raw + " h";
                        break;
                    }

                case "SNVT_mass_kilo":
                    {
                        decimal raw = GetUnsigned(data, offset, 2) / 10;

                        value = "Kilogramm: " + raw;
                        break;
                    }

                case "SNVT_temp":
                    {
                        uint tempRaw = GetUnsigned(data, offset, 2);

                        if (tempRaw != 0xFFFF)
                        {
                            decimal temp = (decimal)(tempRaw - 2740) / 10;

                            value = temp + " °C";
                        }
                        else
                        {
                            value = "(N/A)";
                        }
                        break;
                    }

                case "SNVT_temp_p":
                    {
                        int tempRaw = GetSigned(data, offset, 2);

                        if (tempRaw != 0x7FFF)
                        {
                            decimal temp = (decimal)tempRaw / 100;

                            value = temp + " °C";
                        }
                        else
                        {
                            value = "(N/A)";
                        }
                        break;
                    }

                case "SNVT_lev_percent":
                    {
                        int raw = GetSigned(data, offset, 2);

                        if (raw != 32767)
                        {
                            decimal temp = (decimal)raw / 200;

                            value = "pct: " + temp.ToString("0.00") + " %";
                        }
                        else
                        {
                            value = "pct: (invalid)";
                        }
                        break;
                    }

                case "SNVT_lev_cont":
                    {
                        uint raw = GetUnsigned(data, offset, 1);
                        decimal temp = (decimal)raw / 2;

                        value = "lvl: " + temp + " %";
                        break;
                    }

                case "SNVT_elec_kwh":
                    {
                        uint raw = GetUnsigned(data, offset, 1);
                        value = raw + " kWh";
                        break;
                    }

                case "SNVT_time_stamp":
                    {
                        int year = GetSigned(data, offset + 0, 1);
                        uint month = GetUnsigned(data, offset + 2, 1);
                        uint day = GetUnsigned(data, offset + 3, 1);
                        uint hour = GetUnsigned(data, offset + 4, 1);
                        uint minute = GetUnsigned(data, offset + 5, 1);
                        uint second = GetUnsigned(data, offset + 6, 1);

                        string timeString = hour.ToString("00") + "." + minute.ToString("00") + "." + second.ToString("00");

                        if (year != -1 && year != 0)
                        {
                            value = "" + day.ToString("00") + "." + month.ToString("00") + "." + (year) + " " + timeString;
                        }
                        else
                        {
                            value = "" + day.ToString("00") + "." + month.ToString("00") + ". " + timeString;
                        }

                        break;
                    }

                case "SNVT_address":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        if (raw != 0xFD00)
                        {
                            value = "" + raw;
                        }
                        else
                        {
                            value = "(invalid)";
                        }
                        break;
                    }

                case "SNVT_str_asc":
                    {
                        byte[] local = new byte[length];
                        Array.Copy(data, offset, local, 0, length);
                        value = "'" + Encoding.UTF8.GetString(local).TrimEnd('\0') + "'";
                        break;
                    }

                case "SNVT_state":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        value = "" + raw.ToString("X4");
                        break;
                    }

                case "SNVT_hvac_mode":
                    {
                        uint raw = GetUnsigned(data, offset, 1);
                        value = "Mode " + raw;
                        break;
                    }

                case "SNVT_obj_status":
                    {
                        uint obj = GetUnsigned(data, offset + 0, 2);
                        uint flags = GetUnsigned(data, offset + 2, 4);

                        value = "Obj " + obj + " Status " + flags.ToString("X4");
                        break;
                    }

                case "SNVT_switch":
                    {
                        uint rawValue = GetUnsigned(data, offset + 0, 1);
                        int rawState = GetSigned(data, offset + 1, 1);

                        value = "Value: " + rawValue + ", State: " + rawState;
                        break;
                    }

                case "SNVT_lev_disc":
                    {
                        uint raw = GetUnsigned(data, offset, 1);

                        value = "" + raw;
                        break;
                    }

                case "SNVT_time_min":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        value = "" + raw + " min";
                        break;
                    }

                case "SNVT_time_sec":
                    {
                        uint raw = GetUnsigned(data, offset, 2);

                        if (raw != 65535)
                        {
                            value = "" + raw + " s";
                        }
                        else
                        {
                            value = "(invalid)";
                        }
                        break;
                    }

                case "":
                case "SNVT_obj_request":
                case "UNVT":
                    {
                        value = "";
                        break;
                    }

                default:
                    {
                        value = "unknown: " + type.Name;
                        break;
                    }
            }

            return value;
        }

        private static uint GetUnsigned(byte[] data, int offset, int length)
        {
            byte[] converted = new byte[length];
            Array.Copy(data, offset, converted, 0, length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(converted);
            }

            switch (length)
            {
                case 1:
                    return converted[0];
                case 2:
                    return BitConverter.ToUInt16(converted, 0);
                case 4:
                    return BitConverter.ToUInt32(converted, 0);

                default:
                    return 0;
            }
        }

        private static int GetSigned(byte[] data, int offset, int length)
        {
            byte[] converted = new byte[length];
            Array.Copy(data, offset, converted, 0, length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(converted);
            }

            switch (length)
            {
                case 1:
                    return (sbyte)converted[0];
                case 2:
                    return BitConverter.ToInt16(converted, 0);
                case 4:
                    return BitConverter.ToInt32(converted, 0);

                default:
                    return 0;
            }
        }

        /* from https://www.lonmark.org/nvs/ */
        static LonStandardTypes()
        {
            Types.AddRange(
                new[] {
                    new LonType(0  , "UNVT"                     ),
                    new LonType(1  , "SNVT_amp"                 ),
                    new LonType(2  , "SNVT_amp_mil"             ),
                    new LonType(3  , "SNVT_angle"               ),
                    new LonType(4  , "SNVT_angle_vel"           ),
                    new LonType(5  , "SNVT_btu_kilo"            ),
                    new LonType(6  , "SNVT_btu_mega"            ),
                    new LonType(7  , "SNVT_char_ascii"          ),
                    new LonType(8  , "SNVT_count"               ),
                    new LonType(9  , "SNVT_count_inc"           ),
                    new LonType(10 , "SNVT_date_cal"            ),
                    new LonType(11 , "SNVT_date_day"            ),
                    new LonType(12 , "SNVT_date_time"           ),
                    new LonType(13 , "SNVT_elec_kwh"            ),
                    new LonType(14 , "SNVT_elec_whr"            ),
                    new LonType(15 , "SNVT_flow"                ),
                    new LonType(16 , "SNVT_flow_mil"            ),
                    new LonType(17 , "SNVT_length"              ),
                    new LonType(18 , "SNVT_length_kilo"         ),
                    new LonType(19 , "SNVT_length_micr"         ),
                    new LonType(20 , "SNVT_length_mil"          ),
                    new LonType(21 , "SNVT_lev_cont"            ),
                    new LonType(22 , "SNVT_lev_disc"            ),
                    new LonType(23 , "SNVT_mass"                ),
                    new LonType(24 , "SNVT_mass_kilo"           ),
                    new LonType(25 , "SNVT_mass_mega"           ),
                    new LonType(26 , "SNVT_mass_mil"            ),
                    new LonType(27 , "SNVT_power"               ),
                    new LonType(28 , "SNVT_power_kilo"          ),
                    new LonType(29 , "SNVT_ppm"                 ),
                    new LonType(30 , "SNVT_press"               ),
                    new LonType(31 , "SNVT_res"                 ),
                    new LonType(32 , "SNVT_res_kilo"            ),
                    new LonType(33 , "SNVT_sound_db"            ),
                    new LonType(34 , "SNVT_speed"               ),
                    new LonType(35 , "SNVT_speed_mil"           ),
                    new LonType(36 , "SNVT_str_asc"             ),
                    new LonType(37 , "SNVT_str_int"             ),
                    new LonType(38 , "SNVT_telcom"              ),
                    new LonType(39 , "SNVT_temp"                ),
                    new LonType(40 , "SNVT_time_passed"         ),
                    new LonType(41 , "SNVT_vol"                 ),
                    new LonType(42 , "SNVT_vol_kilo"            ),
                    new LonType(43 , "SNVT_vol_mil"             ),
                    new LonType(44 , "SNVT_volt"                ),
                    new LonType(45 , "SNVT_volt_dbmv"           ),
                    new LonType(46 , "SNVT_volt_kilo"           ),
                    new LonType(47 , "SNVT_volt_mil"            ),
                    new LonType(48 , "SNVT_amp_f"               ),
                    new LonType(49 , "SNVT_angle_f"             ),
                    new LonType(50 , "SNVT_angle_vel_f"         ),
                    new LonType(51 , "SNVT_count_f"             ),
                    new LonType(52 , "SNVT_count_inc_f"         ),
                    new LonType(53 , "SNVT_flow_f"              ),
                    new LonType(54 , "SNVT_length_f"            ),
                    new LonType(55 , "SNVT_lev_cont_f"          ),
                    new LonType(56 , "SNVT_mass_f"              ),
                    new LonType(57 , "SNVT_power_f"             ),
                    new LonType(58 , "SNVT_ppm_f"               ),
                    new LonType(59 , "SNVT_press_f"             ),
                    new LonType(60 , "SNVT_res_f"               ),
                    new LonType(61 , "SNVT_sound_db_f"          ),
                    new LonType(62 , "SNVT_speed_f"             ),
                    new LonType(63 , "SNVT_temp_f"              ),
                    new LonType(64 , "SNVT_time_f"              ),
                    new LonType(65 , "SNVT_vol_f"               ),
                    new LonType(66 , "SNVT_volt_f"              ),
                    new LonType(67 , "SNVT_btu_f"               ),
                    new LonType(68 , "SNVT_elec_whr_f"          ),
                    new LonType(69 , "SNVT_config_src"          ),
                    new LonType(70 , "SNVT_color"               ),
                    new LonType(71 , "SNVT_grammage"            ),
                    new LonType(72 , "SNVT_grammage_f"          ),
                    new LonType(73 , "SNVT_file_req"            ),
                    new LonType(74 , "SNVT_file_status"         ),
                    new LonType(75 , "SNVT_freq_f"              ),
                    new LonType(76 , "SNVT_freq_hz"             ),
                    new LonType(77 , "SNVT_freq_kilohz"         ),
                    new LonType(78 , "SNVT_freq_milhz"          ),
                    new LonType(79 , "SNVT_lux"                 ),
                    new LonType(80 , "SNVT_ISO_7811"            ),
                    new LonType(81 , "SNVT_lev_percent"         ),
                    new LonType(82 , "SNVT_multiplier"          ),
                    new LonType(83 , "SNVT_state"               ),
                    new LonType(84 , "SNVT_time_stamp"          ),
                    new LonType(85 , "SNVT_zerospan"            ),
                    new LonType(86 , "SNVT_magcard"             ),
                    new LonType(87 , "SNVT_elapsed_tm"          ),
                    new LonType(88 , "SNVT_alarm"               ),
                    new LonType(89 , "SNVT_currency"            ),
                    new LonType(90 , "SNVT_file_pos"            ),
                    new LonType(91 , "SNVT_muldiv"              ),
                    new LonType(92 , "SNVT_obj_request"         ),
                    new LonType(93 , "SNVT_obj_status"          ),
                    new LonType(94 , "SNVT_preset"              ),
                    new LonType(95 , "SNVT_switch"              ),
                    new LonType(96 , "SNVT_trans_table"         ),
                    new LonType(97 , "SNVT_override"            ),
                    new LonType(98 , "SNVT_pwr_fact"            ),
                    new LonType(99 , "SNVT_pwr_fact_f"          ),
                    new LonType(100, "SNVT_density"             ),
                    new LonType(101, "SNVT_density_f"           ),
                    new LonType(102, "SNVT_rpm"                 ),
                    new LonType(103, "SNVT_hvac_emerg"          ),
                    new LonType(104, "SNVT_angle_deg"           ),
                    new LonType(105, "SNVT_temp_p"              ),
                    new LonType(106, "SNVT_temp_setpt"          ),
                    new LonType(107, "SNVT_time_sec"            ),
                    new LonType(108, "SNVT_hvac_mode"           ),
                    new LonType(109, "SNVT_occupancy"           ),
                    new LonType(110, "SNVT_area"                ),
                    new LonType(111, "SNVT_hvac_overid"         ),
                    new LonType(112, "SNVT_hvac_status"         ),
                    new LonType(113, "SNVT_press_p"             ),
                    new LonType(114, "SNVT_address"             ),
                    new LonType(115, "SNVT_scene"               ),
                    new LonType(116, "SNVT_scene_cfg"           ),
                    new LonType(117, "SNVT_setting"             ),
                    new LonType(118, "SNVT_evap_state"          ),
                    new LonType(119, "SNVT_therm_mode"          ),
                    new LonType(120, "SNVT_defr_mode"           ),
                    new LonType(121, "SNVT_defr_term"           ),
                    new LonType(122, "SNVT_defr_state"          ),
                    new LonType(123, "SNVT_time_min"            ),
                    new LonType(124, "SNVT_time_hour"           ),
                    new LonType(125, "SNVT_ph"                  ),
                    new LonType(126, "SNVT_ph_f"                ),
                    new LonType(127, "SNVT_chlr_status"         ),
                    new LonType(128, "SNVT_tod_event"           ),
                    new LonType(129, "SNVT_smo_obscur"          ),
                    new LonType(130, "SNVT_fire_test"           ),
                    new LonType(131, "SNVT_temp_ror"            ),
                    new LonType(132, "SNVT_fire_init"           ),
                    new LonType(133, "SNVT_fire_indcte"         ),
                    new LonType(134, "SNVT_time_zone"           ),
                    new LonType(135, "SNVT_earth_pos"           ),
                    new LonType(136, "SNVT_reg_val"             ),
                    new LonType(137, "SNVT_reg_val_ts"          ),
                    new LonType(138, "SNVT_volt_ac"             ),
                    new LonType(139, "SNVT_amp_ac"              ),
                    new LonType(143, "SNVT_turbidity"           ),
                    new LonType(144, "SNVT_turbidity_f"         ),
                    new LonType(145, "SNVT_hvac_type"           ),
                    new LonType(146, "SNVT_elec_kwh_l"          ),
                    new LonType(147, "SNVT_temp_diff_p"         ),
                    new LonType(148, "SNVT_ctrl_req"            ),
                    new LonType(149, "SNVT_ctrl_resp"           ),
                    new LonType(150, "SNVT_ptz"                 ),
                    new LonType(151, "SNVT_privacyzone"         ),
                    new LonType(152, "SNVT_pos_ctrl"            ),
                    new LonType(153, "SNVT_enthalpy"            ),
                    new LonType(154, "SNVT_gfci_status"         ),
                    new LonType(155, "SNVT_motor_state"         ),
                    new LonType(156, "SNVT_pumpset_mn"          ),
                    new LonType(157, "SNVT_ex_control"          ),
                    new LonType(158, "SNVT_pumpset_sn"          ),
                    new LonType(159, "SNVT_pump_sensor"         ),
                    new LonType(160, "SNVT_abs_humid"           ),
                    new LonType(161, "SNVT_flow_p"              ),
                    new LonType(162, "SNVT_dev_c_mode"          ),
                    new LonType(163, "SNVT_valve_mode"          ),
                    new LonType(164, "SNVT_alarm_2"             ),
                    new LonType(165, "SNVT_state_64"            ),
                    new LonType(166, "SNVT_nv_type"             ),
                    new LonType(168, "SNVT_ent_opmode"          ),
                    new LonType(169, "SNVT_ent_state"           ),
                    new LonType(170, "SNVT_ent_status"          ),
                    new LonType(171, "SNVT_flow_dir"            ),
                    new LonType(172, "SNVT_hvac_satsts"         ),
                    new LonType(173, "SNVT_dev_status"          ),
                    new LonType(174, "SNVT_dev_fault"           ),
                    new LonType(175, "SNVT_dev_maint"           ),
                    new LonType(176, "SNVT_date_event"          ),
                    new LonType(177, "SNVT_sched_val"           ),
                    new LonType(178, "SNVT_sec_state"           ),
                    new LonType(179, "SNVT_sec_status"          ),
                    new LonType(180, "SNVT_sblnd_state"         ),
                    new LonType(181, "SNVT_rac_ctrl"            ),
                    new LonType(182, "SNVT_rac_req"             ),
                    new LonType(183, "SNVT_count_32"            ),
                    new LonType(184, "SNVT_clothes_w_c"         ),
                    new LonType(185, "SNVT_clothes_w_m"         ),
                    new LonType(186, "SNVT_clothes_w_s"         ),
                    new LonType(187, "SNVT_clothes_w_a"         ),
                    new LonType(188, "SNVT_multiplier_s"        ),
                    new LonType(189, "SNVT_switch_2"            ),
                    new LonType(190, "SNVT_color_2"             ),
                    new LonType(191, "SNVT_log_status"          ),
                    new LonType(192, "SNVT_time_stamp_p"        ),
                    new LonType(193, "SNVT_log_fx_request"      ),
                    new LonType(194, "SNVT_log_fx_status"       ),
                    new LonType(195, "SNVT_log_request"         ),
                    new LonType(196, "SNVT_enthalpy_d"          ),
                    new LonType(197, "SNVT_amp_ac_mil"          ),
                    new LonType(198, "SNVT_time_hour_p"         ),
                    new LonType(199, "SNVT_lamp_status"         ),
                    new LonType(200, "SNVT_environment"         ),
                    new LonType(201, "SNVT_geo_loc"             ),
                    new LonType(202, "SNVT_program_status"      ),
                    new LonType(203, "SNVT_load_offsets"        ),
                    new LonType(204, "SNVT_Wm2_p"               ),
                    new LonType(205, "SNVT_safe_1"              ),
                    new LonType(206, "SNVT_safe_2"              ),
                    new LonType(207, "SNVT_safe_4"              ),
                    new LonType(208, "SNVT_safe_8"              ),
                    new LonType(209, "SNVT_time_val_2"          ),
                    new LonType(210, "SNVT_time_offset"         ),
                    new LonType(211, "SNVT_sched_exc"           ),
                    new LonType(212, "SNVT_sched_status"        ),
                    new LonType(213, "SNVT_mass_flow"           ),
                    new LonType(214, "SNVT_mass_flow_f"         ),
                    new LonType(215, "SNVT_time_min_p"          ),
                    new LonType(216, "SNVT_stat_control"        ),
                    new LonType(217, "SNVT_faults"              ),
                    new LonType(218, "SNVT_control_data"        ),
                    new LonType(219, "SNVT_power_profile"       ),
                    new LonType(220, "SNVT_version"             ),
                    new LonType(221, "SNVT_control_cfg"         ),
                    new LonType(222, "SNVT_fault_limits"        ),
                    new LonType(223, "SNVT_scene_def"           ),
                    new LonType(224, "SNVT_lux_2"               ),
                    new LonType(225, "SNVT_temp_setpt_2"        )
                });
        }
    }
}
