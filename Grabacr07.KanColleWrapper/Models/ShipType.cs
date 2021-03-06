﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Models.Raw;
using Grabacr07.KanColleWrapper.Internal;

namespace Grabacr07.KanColleWrapper.Models
{
	/// <summary>
	/// 艦種を表します。
	/// </summary>
	public class ShipType : RawDataWrapper<kcsapi_mst_stype>, IIdentifiable
	{
		public int Id
		{
			get { return this.RawData.api_id; }
		}

		public string Name
		{
			get { return KanColleClient.Current.Translations.GetTranslation(RawData.api_name, TranslationType.ShipTypes, this.RawData, this.Id); }
		}

		public int SortNumber
		{
			get { return this.RawData.api_sortno; }
		}

		public double RepairMultiplier
		{
			get
			{
				switch ((ShipTypeId)this.Id)
				{
					case ShipTypeId.Submarine:
						return 0.5;
					case ShipTypeId.HeavyCruiser:
					case ShipTypeId.AerialCruiser:
					case ShipTypeId.FastBattleship:
					case ShipTypeId.LightAircraftCarrier:
					case ShipTypeId.SubmarineTender:
						return 1.5;
					case ShipTypeId.Battleship:
					case ShipTypeId.Superdreadnought:
					case ShipTypeId.AerialBattleship:
					case ShipTypeId.AircraftCarrier:
					case ShipTypeId.ArmoredAircraftCarrier:
					case ShipTypeId.RepairShip:
						return 2;
					default:
						return 1;
				}
			}
		}

		public ShipType(kcsapi_mst_stype rawData) : base(rawData) { }

		public override string ToString()
		{
			return string.Format("ID = {0}, Name = \"{1}\"", this.Id, this.Name);
		}

		#region static members

		private static ShipType dummy = new ShipType(new kcsapi_mst_stype
		{
			api_id = 999,
			api_sortno = 999,
			api_name = "不審船",
		});

		public static ShipType Dummy
		{
			get { return dummy; }
		}

		#endregion
	}
}
