using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalLib.Core.Sensors
{
    public enum LE_DEVICESTATUS_ID
    {
        LE_DEVICESTATUS_ID_BASE = 0x0400 + 4000,
        LE_DEVICESTATUS_ID_INVALID,
        SENSOR_PCMODE,
        SENSOR_PCMODE_ACQUIRE
    }

    public enum SENSOR_FORM
    {
        FF_INVALID = 0,
        FF_PC,
        FF_BOOKLEFT,
        FF_BOOKRIGHT,
        FF_STAND,
        FF_TENT,
        FF_TABLETB,
        FF_TABLETC,
        FF_FLAT,
        FF_COUNT
    }

    public enum LibStatusType
    {
        SENSOR_EVENT = 1,
        BATTERY_EVENT = 2,
        SESSION_EVENT = 4,
        STYLUS_EVENT = 8
    }
}
