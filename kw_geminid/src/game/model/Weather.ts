export enum WeatherType {
  Sunny = 'Sunny',       // 晴天
  Cloudy = 'Cloudy',     // 阴天
  Rainy = 'Rainy',       // 雨天
  HeavyRain = 'HeavyRain', // 豪雨
  Snowy = 'Snowy'        // 雪天
}

export interface WeatherInfo {
  type: WeatherType;
  name: string;
  fireMod: number;   // 火系倍率
  waterMod: number;  // 水系倍率
  windMod: number;   // 风系倍率
  earthMod: number;  // 地系倍率
  allowFire: boolean;
  color: string;
}

export const WEATHER_DATA: Record<WeatherType, WeatherInfo> = {
  [WeatherType.Sunny]: {
    type: WeatherType.Sunny,
    name: '晴天',
    fireMod: 1.2,
    waterMod: 0.8,
    windMod: 1.0,
    earthMod: 1.0,
    allowFire: true,
    color: '#ffcc44'
  },
  [WeatherType.Cloudy]: {
    type: WeatherType.Cloudy,
    name: '阴天',
    fireMod: 1.0,
    waterMod: 1.0,
    windMod: 1.2,
    earthMod: 1.0,
    allowFire: true,
    color: '#a0aab8'
  },
  [WeatherType.Rainy]: {
    type: WeatherType.Rainy,
    name: '雨天',
    fireMod: 0.7,
    waterMod: 1.3,
    windMod: 1.1,
    earthMod: 1.0,
    allowFire: true,
    color: '#5599dd'
  },
  [WeatherType.HeavyRain]: {
    type: WeatherType.HeavyRain,
    name: '豪雨',
    fireMod: 0.0,
    waterMod: 1.6,
    windMod: 1.2,
    earthMod: 0.9,
    allowFire: false,
    color: '#3366bb'
  },
  [WeatherType.Snowy]: {
    type: WeatherType.Snowy,
    name: '雪天',
    fireMod: 0.8,
    waterMod: 1.2,
    windMod: 1.0,
    earthMod: 1.1,
    allowFire: true,
    color: '#e8f4f8'
  }
};
