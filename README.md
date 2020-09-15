# Coocoo3D
一个MMD渲染器，支持DirectX12和DXR光线追踪，运行速度超快 ~~（实测i7-9750H + RTX2070max-q 单人物模型加载动作 加载天空盒 最高达1100fps）。~~ RTX2070max-q单模型多线程渲染能稳定800fps

视频及演示下载[https://www.bilibili.com/video/BV1p54y127ig/](https://www.bilibili.com/video/BV1p54y127ig/)

DXR为初步支持，支持AO和反射。

支持自定义着色器，支持使用模型面元的GPU粒子。
## 功能
* 播放动画
* 加载pmx模型
* 加载vmd动作
* 自定义着色器
* GPU粒子系统
## 特征
* 异步加载物体
* 缓存物体
* 无阻塞UI
## 高效架构
Coocoo3D的开发使用了一种新的软件开发模式。组件间的联系不从加载时开始，不在卸载时结束，这样各个组件的引用的生存周期可以变长，可以简化组件管理代码的编写。

Coocoo3D从不让用户等待太久，因此Coocoo3D比较耗时的加载过程都是渐进的。
