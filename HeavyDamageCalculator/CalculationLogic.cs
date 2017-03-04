using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeavyDamageCalculator {
	/// <summary>
	/// プロット用データにおける各点のデータ
	/// </summary>
	struct Point {
		public double X { get; set; }
		public double Y { get; set; }
	}
	// グラフデータを管理するための構造体
	class GraphParameter {
		public string Name;
		public int MaxHp;
		public int Armor;
		public int NowHp;
		public bool NaiveFlg;
		public GraphParameter(string name, int maxHp, int armor, int nowHp, bool naiveFlg) {
			Name = name;
			MaxHp = maxHp;
			Armor = armor;
			NowHp = nowHp;
			NaiveFlg = naiveFlg;
		}
	}
	/// <summary>
	/// 大破率計算ロジック
	/// </summary>
	static class CalculationLogic {
		#region 定数定義
		// 装甲乱数における最小値と範囲と最大値の倍率
		const double MinArmorPer = 0.7;
		const double RangeArmorPer = 0.6;
		const double MaxArmorPer = MinArmorPer + RangeArmorPer;
		// カスダメにおける最小値と範囲の倍率
		const double MinVeryLightPer = 0.06;
		const double RangeVeryLightPer = 0.08;
		// 轟沈ストッパーにおける最小値と範囲の倍率
		const double MinStopperPer = 0.5;
		const double RangeStopperPer = 0.3;
		#endregion
		/// <summary>
		/// プロット用データを用意する
		/// </summary>
		/// <param name="maxHp">最大の耐久値</param>
		/// <param name="armor">装甲値</param>
		/// <param name="nowHp">現在の耐久値</param>
		/// <returns>プロット用データ</returns>
		public static List<Point> CalcPlotData(int maxHp, int armor, int nowHp, bool naiveFlg) {
			if(naiveFlg) {
				return CalcPlotDataNaive(maxHp, armor, nowHp);
			} else {
				return CalcPlotDataOriginal(maxHp, armor, nowHp);
			}
		}
		public static List<Point> CalcPlotData(GraphParameter g) {
			return CalcPlotData(g.MaxHp, g.Armor, g.NowHp, g.NaiveFlg);
		}
		/// <summary>
		/// プロット用データを用意する(オリジナル版)
		/// </summary>
		/// <param name="maxHp">最大の耐久値</param>
		/// <param name="armor">装甲値</param>
		/// <param name="nowHp">現在の耐久値</param>
		/// <returns>プロット用データ</returns>
		public static List<Point> CalcPlotDataOriginal(int maxHp, int armor, int nowHp) {
			// 大破判定を受ける最大の耐久値
			int heavyDamageHp = maxHp / 4;
			// 装甲乱数の最小値と範囲と最大値
			double armorMin = armor * MinArmorPer;
			double armorRange = armor * RangeArmorPer;
			double armorMax = armor * MaxArmorPer;
			/// 各種攻撃力
			/// (power[4]以外は、armor_rangeを足すと装甲最大時のものになる)
			/// power[0] : 計算用に左に飛ばしている
			/// power[1] : 装甲最小時、カスダメとなる上限
			/// power[2] : 装甲最小時、中破以下となる上限
			/// power[3] : 装甲最小時、大破となる上限
			/// power[4] : 装甲最大時、大破となる上限
			var power = new double[]{
				-armorRange,
				armorMin,
				armorMin + (nowHp > heavyDamageHp + 1 ? nowHp - heavyDamageHp - 1 : 0),
				armorMin + nowHp - 1,
				armorMax + nowHp - 1,
			};
			/// 各種領域の確率
			/// area_prob[0] : カスダメ時の大破率
			/// area_prob[1] : 装甲を抜いて中破以下になった時の大破率(0.0)
			/// area_prob[2] : 装甲を抜いて大破した時の大破率(1.0)
			/// area_prob[3] : 轟沈ストッパー発動時の大破率(calc_heavy_damage_prob関数)
			var areaProb = new double[]{
				calcVerylightDamageProb(nowHp, heavyDamageHp),
				0.0,
				1.0,
				calcStopperDamageProb(nowHp, heavyDamageHp),
			};
			// x軸を算出
			var node = new List<double>();
			for(int i = 1; i <= 3; ++i) {
				node.Add(power[i]);
				node.Add(power[i] + armorRange);
			}
			// 書き込み用データを用意
			var output = new List<Point>();
			// y軸を算出してoutputに書き込む
			foreach(var n in node) {
				var prob = integrateProbArea(n, armorRange, power, areaProb);
				output.Add(new Point { X = n, Y = prob });
			}
			// x軸の値でソート
			output.Sort((p, q) => Math.Sign(p.X - q.X));
			return output;
		}
		/// <summary>
		/// カスダメ時の大破率を算出する
		/// </summary>
		/// <param name="nowHp">現在の耐久値</param>
		/// <param name="heavyDamageHp">大破判定を受ける最大の耐久値</param>
		/// <returns>カスダメ時の大破率</returns>
		static double calcVerylightDamageProb(int nowHp, int heavyDamageHp) {
			// 大破した回数をカウントする
			int count = 0;
			for(int hi = 0; hi < nowHp; ++hi) {
				// カスダメ時のダメージ
				int damage = (int)(MinVeryLightPer * nowHp + RangeVeryLightPer * hi);
				// カスダメ時の残耐久
				int leave_hp = nowHp - damage;
				// 大破判定
				if(leave_hp <= heavyDamageHp)
					++count;
			}
			// カウントから大破率を計算する
			return 1.0 * count / nowHp;
		}
		/// <summary>
		/// 轟沈ストッパー発動時の大破率を算出する
		/// </summary>
		/// <param name="nowHp">現在の耐久値</param>
		/// <param name="heavyDamageHp">大破判定を受ける最大の耐久値</param>
		/// <returns>轟沈ストッパー発動時の大破率</returns>
		static double calcStopperDamageProb(int nowHp, int heavyDamageHp) {
			// 大破した回数をカウントする
			int count = 0;
			for(int hi = 0; hi < nowHp; ++hi) {
				// カスダメ時のダメージ
				int damage = (int)(MinStopperPer * nowHp + RangeStopperPer * hi);
				// カスダメ時の残耐久
				int leave_hp = nowHp - damage;
				// 大破判定
				if(leave_hp <= heavyDamageHp)
					++count;
			}
			// カウントから大破率を計算する
			return 1.0 * count / nowHp;
		}
		/// <summary>
		/// 入力値を[0.0, 1.0]の範囲に制限する
		/// </summary>
		/// <param name="x">入力値</param>
		/// <returns>制限後の入力値</returns>
		static double compressor(double x) {
			return (x < 0.0 ? 0.0 : x > 1.0 ? 1.0 : x);
		}
		/// <summary>
		/// 確率領域の積分を実行する
		/// </summary>
		/// <param name="attack">最終攻撃力</param>
		/// <param name="armorRange">装甲乱数の幅</param>
		/// <param name="power">各種攻撃力</param>
		/// <param name="areaProb">各種領域の確率</param>
		/// <returns></returns>
		static double integrateProbArea(double attack, double armorRange, double[] power, double[] areaProb) {
			double rc = 0.0;
			for(int i = 0; i < 4; ++i) {
				// 上限
				var prob1 = compressor((attack - power[i]) / armorRange);
				// 下限
				var prob2 = compressor((attack - power[i + 1]) / armorRange);
				// 積分計算
				rc += (prob1 - prob2) * areaProb[i];
			}
			return rc;
		}
		/// <summary>
		/// プロット用データを用意する(ナイーブ版)
		/// </summary>
		/// <param name="maxHp">最大の耐久値</param>
		/// <param name="armor">装甲値</param>
		/// <param name="nowHp">現在の耐久値</param>
		/// <returns>プロット用データ</returns>
		public static List<Point> CalcPlotDataNaive(int maxHp, int armor, int nowHp) {
			// 書き込み用データを用意
			var output = new List<Point>();
			// 大破判定を受ける最大の耐久値
			int heavyDamageHp = maxHp / 4;
			// 確実にカスダメとなる最大の最終攻撃力
			int maxVeryLightPower = (int)(Math.Ceiling(armor * MinArmorPer));
			// 確実に轟沈ストッパーが掛かる最小の最終攻撃力
			int minStopperPower = nowHp + (int)(Math.Ceiling(armor * MinArmorPer + (armor - 1) * RangeArmorPer));
			// カスダメ時の大破率
			var verylightProb = calcVerylightDamageProb(nowHp, heavyDamageHp);
			// 轟沈ストッパー時の大破率
			var stopperProb = calcStopperDamageProb(nowHp, heavyDamageHp);
			// 最終攻撃力がmaxVeryLightPowerの場合から順に計算していく
			for(double x = maxVeryLightPower; x <= minStopperPower; x += 0.1) {
					double heavyDamagePer = 0.0;
				// 装甲乱数
				for(int ai = 0; ai < armor; ++ai) {
					double armor_rand = 0.7 * armor + 0.6 * ai;
					int damage = (int)(x - armor_rand);
					if(damage <= 0) {
						// カスダメ時の処理
						heavyDamagePer += verylightProb / armor;
						continue;
					}
					int leave_hp = nowHp - damage;
					if(leave_hp <= 0) {
						// 轟沈ストッパー時の処理
						heavyDamagePer += stopperProb / armor;
						continue;
					}
					// 通常のダメージ処理
					if(leave_hp <= heavyDamageHp) {
						heavyDamagePer += 1.0 / armor;
					}
				}
				// データ追加
				output.Add(new Point { X = x, Y = heavyDamagePer });
			}
			return output;
		}
	}
}
