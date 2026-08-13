// ============================================================
// Dissolve Shader — 敌人死亡溶解特效
// 技术方案：噪声驱动 AlphaClip + 边缘发光
// 编写方式：ShaderLab（声明结构）+ HLSL（渲染逻辑）
// ============================================================

Shader "Custom/Dissolve"
{
    // ============================================================
    // Properties — 参数声明区
    // 这里声明的参数会出现在材质 Inspector 面板上，可手动调整或脚本运行时修改
    //
    // Shader 支持的参数类型：
    //   Float    — 浮点数（如 0.5）
    //   Range    — 带范围的浮点数（如 Range(0,1)，Inspector 显示为滑条）
    //   Int      — 整数
    //   Color    — RGBA 颜色（4 个浮点，Inspector 显示取色器）
    //   Vector   — 4 维向量（x,y,z,w，Inspector 显示 4 个输入框）
    //   2D       — 2D 贴图（Inspector 显示贴图槽位）
    //   Cube     — 立方体贴图（6 面天空盒）
    //   3D       — 3D 体积贴图
    //   Any      — 任意类型贴图（不限制维度）
    //
    // 语法：_变量名 ("Inspector显示名", 类型) = 默认值
    // ============================================================
    Properties
    {
        // 贴图：敌人的原始外观贴图。溶解时需要显示敌人长什么样
        // "white" 表示默认值为纯白色贴图（未指定贴图时显示白色）
        _BaseMap ("Base Map", 2D) = "white" {}

        // 染色：乘到贴图上的颜色。白色(1,1,1,1)表示不染色，原样显示贴图
        // 如果想给敌人统一偏色，改这个参数即可
        _BaseColor ("Base Color", Color) = (1,1,1,1)

        // 溶解进度：0 = 完全可见，1 = 完全消失
        // 脚本每帧 SetFloat 把这个值从 0 逐渐拉到 1，驱动溶解动画
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0

        // 发光边缘宽度：溶解边界处发光区域的厚度
        // 0.05 = 细边，0.3 = 宽边。值越大发光范围越宽
        _EdgeWidth ("Edge Width", Range(0,0.5)) = 0.1

        // 边缘发光颜色：溶解边界显示的颜色
        // 橙红色 (1,0.4,0,1) 模拟烧蚀效果
        // 改成蓝色可以做"冰冻溶解"，绿色可以做"毒蚀溶解"
        _EdgeColor ("Edge Color", Color) = (1,0.4,0,1)

        // 噪声密度：控制溶解形状的细密程度
        // 10 = 粗大块状溶解，30 = 中等颗粒，100 = 细碎沙粒状
        _NoiseScale ("Noise Scale", Range(1,100)) = 30
    }

    // ============================================================
    // SubShader — 子着色器
    // 一个 Shader 可以包含多个 SubShader，引擎会按顺序尝试，
    // 选择当前硬件/渲染管线支持的第一个来使用。
    // 一般只需一个 SubShader，多 SubShader 用于跨平台兼容
    // （如 PC 用高精度、移动端用低精度）。
    // ============================================================
    SubShader
    {
        // ========================================================
        // Tags — 渲染标签
        // 告诉引擎这个 Shader 应该在渲染管线的哪个阶段执行，
        // 以及如何分类和排序
        //
        // 常见 Tags：
        //   "RenderType" = "Opaque"          → 不透明物体（墙体、角色）
        //   "RenderType" = "Transparent"     → 半透明物体（玻璃、水）
        //   "RenderPipeline" = "UniversalPipeline" → URP 专用
        //   "RenderPipeline" = "HDRP"        → HDRP 专用
        //   "Queue" = "Geometry"             → 渲染队列：几何体（默认 2000）
        //   "Queue" = "Transparent"          → 渲染队列：半透明（3000）
        // ========================================================
        Tags
        {
            "RenderType" = "Opaque"              // 标记为不透明物体
            "RenderPipeline" = "UniversalPipeline"  // 标记为 URP 专用（非 Built-in、非 HDRP）
        }

        // Cull Off — 关闭背面剔除
        // 默认 Cull Back（只渲染正面，背面不渲染）
        // 溶解后模型内部会暴露，需要双面渲染才能看到内部
        // 可选值：Cull Back（默认）、Cull Front（只渲染背面）、Cull Off（双面都渲染）
        Cull Off

        // ZWrite On — 开启深度写入
        //
        // 深度缓冲（Z-Buffer / Depth Buffer）：
        //   是一块和屏幕等大的内存，存储每个像素的"距离摄像机的远近"
        //   GPU 渲染每个像素时，先查深度缓冲：
        //     如果当前像素比缓冲中的更近 → 覆盖像素颜色 + 更新深度
        //     如果当前像素更远 → 丢弃（被前面的物体挡住了）
        //   这就是为什么近处的物体会遮挡远处的物体
        //
        // ZWrite On  = 写入深度缓冲（不透明物体，参与遮挡判断）
        // ZWrite Off = 不写入深度缓冲（半透明物体，不遮挡后面的东西）
        ZWrite On

        // ========================================================
        // Pass 1: ForwardLit — 主渲染 Pass
        // 这是实际渲染敌人外观的 Pass，处理贴图、溶解、发光
        // ========================================================
        Pass
        {
            Name "ForwardLit"  // Pass 名称，方便调试时识别

            // Pass 级别的 Tags，指定这个 Pass 在哪个渲染阶段执行
            // "UniversalForward" = URP 的前向渲染阶段（主画面渲染）
            Tags { "LightMode" = "UniversalForward" }

            // AlphaToMask On — 开启 Alpha To Coverage
            // 作用：让 clip() 丢弃的像素在深度缓冲中也标记为透明，
            // 避免溶解边缘的像素深度写入不正确导致渲染顺序错乱
            AlphaToMask On

            HLSLPROGRAM

            // 编译指令：告诉编译器哪个函数是顶点着色器，哪个是片段着色器
            #pragma vertex vert    // vert 函数处理每个顶点
            #pragma fragment frag  // frag 函数处理每个像素

            // 引入 URP 核心库，提供 TransformObjectToHClip 等内置函数
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ====================================================
            // 数据结构定义
            // ====================================================

            // Attributes — 顶点着色器的输入
            // 从模型网格数据自动填充，每个顶点一份
            struct Attributes
            {
                float4 positionOS : POSITION;  // 顶点在模型本地空间的位置坐标
                                               // : POSITION 是语义标记，告诉引擎从网格的顶点位置填充
                float2 uv         : TEXCOORD0; // 顶点的 UV 坐标（用于贴图映射）
                                               // : TEXCOORD0 表示第 0 套 UV
            };

            // Varyings — 顶点着色器的输出，传递给片段着色器
            // GPU 会在顶点之间对这份做插值，片段着色器拿到的是插值后的值
            struct Varyings
            {
                float4 positionCS : SV_POSITION;  // 顶点在裁剪空间的位置（屏幕坐标）
                                                   // : SV_POSITION 是系统语义，GPU 用它确定屏幕位置
                float2 uv         : TEXCOORD0;    // UV 坐标（从顶点原样传递，GPU 自动插值）
            };

            // ====================================================
            // 纹理和参数声明
            // ====================================================

            // TEXTURE2D / SAMPLER 是 URP 的贴图声明宏
            // 等价于 Texture2D 和 SamplerState，但跨平台兼容
            TEXTURE2D(_BaseMap);              // 声明 2D 贴图
            SAMPLER(sampler_BaseMap);         // 声明采样器（控制贴图如何被读取：过滤、包裹模式等）

            // CBUFFER（Constant Buffer）— 常量缓冲区
            // 把材质参数打包到一起传给 GPU，URP 要求这样做以支持 SRP Batcher
            // SRP Batcher：批量合并使用相同 Shader 的渲染调用，减少 CPU 开销
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;     // 贴图的 Tiling(xy) 和 Offset(zw)，控制贴图缩放和平移
                half4  _BaseColor;       // 染色颜色（half4 = 16 位浮点，比 float4 精度低但更快）
                float  _DissolveAmount;  // 溶解进度（脚本每帧修改这个值驱动动画）
                float  _EdgeWidth;       // 边缘发光宽度
                half4  _EdgeColor;        // 边缘发光颜色
                float  _NoiseScale;       // 噪声密度
            CBUFFER_END

            // ====================================================
            // 噪声生成函数
            // ====================================================

            // 伪随机噪声：给定一个 UV 坐标，返回一个固定的 0~1 随机值
            // 同一个 UV 永远返回同一个值（确定性），不同 UV 返回不同的值
            // 原理（GPU Gems 经典实现）：
            //   1. dot(uv, float2(12.9898, 78.233)) — UV 点乘常量向量，得到浮点数
            //   2. sin(...) — 取正弦，映射到 -1~1
            //   3. * 43758.5453 — 放大，让相邻 UV 的值差异足够大
            //   4. frac(...) — 取小数部分，映射到 0~1
            float simpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            // ====================================================
            // 顶点着色器 — 处理每个顶点
            // ====================================================
            // 输入：Attributes（模型网格数据）
            // 输出：Varyings（转换后的屏幕坐标 + UV）
            // 职责：把模型本地坐标转换为屏幕坐标，UV 原样传递
            Varyings vert(Attributes input)
            {
                Varyings output;
                // TransformObjectToHClip：URP 内置函数
                // 把物体空间坐标(Object Space) → 裁剪空间(Clip Space) → 屏幕坐标
                // 内部自动处理 MVP 矩阵变换（Model → View → Projection）
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;  // UV 坐标原样传递给片段着色器
                return output;
            }

            // ====================================================
            // 片段着色器 — 处理每个像素
            // ====================================================
            // 输入：Varyings（顶点插值后的数据）
            // 输出：half4 — 该像素的最终颜色（RGBA）
            // 职责：决定每个像素显示什么颜色（溶解、发光、贴图采样）
            //
            // : SV_Target 表示输出到渲染目标（屏幕）
            half4 frag(Varyings input) : SV_Target
            {
                // 第 1 步：生成噪声值（0~1）
                // UV 乘以 _NoiseScale 放大坐标，让噪声更细密
                // _NoiseScale=10 → 粗大块状，_NoiseScale=30 → 中等颗粒，_NoiseScale=100 → 细碎沙粒
                float noise = simpleNoise(input.uv * _NoiseScale);

                // 第 2 步：Alpha Clip — 溶解的核心
                // clip(x)：如果 x < 0，丢弃当前像素（不渲染）
                // 这里 x = noise - _DissolveAmount
                //   _DissolveAmount=0 → 所有 noise(0~1) > 0 → 全部可见
                //   _DissolveAmount=0.5 → noise < 0.5 的像素被丢弃 → 半溶解
                //   _DissolveAmount=1 → 所有 noise < 1 → 全部丢弃 → 完全消失
                clip(noise - _DissolveAmount);

                // 第 3 步：计算边缘发光因子
                // noise - _DissolveAmount = 当前像素距溶解边界的距离
                //   正值 = 在边界外侧（存活区域），距离越远越不发光
                //   0 = 正好在边界上
                // abs() 取绝对值，无论在边界哪侧都为正
                float dist = abs(noise - _DissolveAmount);

                // 归一化到 0~1：
                //   dist/_EdgeWidth → 0 = 正在边界，>1 = 远离边界
                //   1.0 - saturate(...) → 1 = 在边界（强发光），0 = 远离边界（不发光）
                //   saturate() 把值限制在 0~1，防止除法结果超出范围
                float edgeFactor = 1.0 - saturate(dist / _EdgeWidth);

                // 第 4 步：采样贴图（读取贴图在当前 UV 位置的颜色）
                // SAMPLE_TEXTURE2D = URP 的贴图采样宏
                // 采样结果 × _BaseColor = 最终基础颜色（染色）
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // 第 5 步：混合基础色和发光色
                // lerp(A, B, T) = A * (1-T) + B * T
                //   edgeFactor=0 → 显示 baseColor（远离边界，正常区域）
                //   edgeFactor=1 → 显示 _EdgeColor（在边界上，强发光）
                //   中间值 → 两种颜色平滑过渡
                half4 finalColor = lerp(baseColor, _EdgeColor, edgeFactor);

                return finalColor;
            }

            ENDHLSL
        }

        // ========================================================
        // Pass 2: ShadowCaster — 阴影渲染 Pass
        // Unity 渲染阴影时会执行这个 Pass，把物体的深度写入阴影贴图
        // 必须和主 Pass 使用相同的 clip 逻辑，否则阴影形状和溶解形状不一致
        // （溶解了一半但阴影还是完整的，会很违和）
        // ========================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }  // 在阴影渲染阶段执行
            AlphaToMask On
            ZWrite On        // 阴影 Pass 必须写深度
            ZTest LEqual     // 深度测试：小于等于缓冲值时通过（近处遮挡远处）

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            // ShadowCaster Pass 只需要溶解相关的参数，不需要贴图和颜色
            float _DissolveAmount;
            float _NoiseScale;

            float simpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            // 阴影 Pass 的片段着色器
            // 返回 half 而非 half4，因为阴影只需要深度，不需要颜色
            half frag(Varyings input) : SV_Target
            {
                // 和主 Pass 完全相同的溶解逻辑
                float noise = simpleNoise(input.uv * _NoiseScale);
                clip(noise - _DissolveAmount);  // 同步裁剪，阴影跟随溶解
                return 0;  // 阴影不需要颜色输出，只写深度
            }
            ENDHLSL
        }
    }
}
