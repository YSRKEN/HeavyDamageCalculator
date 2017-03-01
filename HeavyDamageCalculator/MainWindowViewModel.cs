using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeavyDamageCalculator {
	class MainWindowViewModel : BindableBase {
		// 最大耐久
		int maxHpValue = 35;
		public int MaxHpValue {
			get { return maxHpValue; }
			set {
				// 現在耐久より小さければ、現在耐久の値を小さくする
				if(nowHpValue > value) {
					NowHpValue = value;
				}
				this.SetProperty(ref this.maxHpValue, value);
			}
		}
		// 装甲
		int armorValue = 49;
		public int ArmorValue {
			get { return armorValue; }
			set {
				this.SetProperty(ref this.armorValue, value);
			}
		}
		// 現在耐久
		int nowHpValue = 35;
		public int NowHpValue {
			get { return nowHpValue; }
			set {
				// 最大耐久より大きければ、最大耐久の値を大きくする
				if(maxHpValue < value) {
					MaxHpValue = value;
				}
				this.SetProperty(ref this.nowHpValue, Math.Min(maxHpValue, value));
			}
		}
		// パラメーター名
		string parameterName = "";
		public string ParameterName {
			get {
				return parameterName;
			}
			set {
				this.SetProperty(ref this.parameterName, value);
			}
		}
		// ウィンドウの大きさ
		double scaleX = 1.0;
		public double ScaleX {
			get {
				return scaleX;
			}
			set {
				this.SetProperty(ref this.scaleX, value);
			}
		}
		double scaleY = 1.0;
		public double ScaleY {
			get {
				return scaleY;
			}
			set {
				this.SetProperty(ref this.scaleY, value);
			}
		}
	}
}
