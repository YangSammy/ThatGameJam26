# V17 第三方来源与许可边界

本目录保留生产工程已采用部分的作者、官方来源、原始下载哈希及原许可文本。记录来自 `BedroomProduction/ThirdParty` 中的实际来源文件；本次整理没有重新下载或领取资产，也不包含游戏参考图、商店宣传图或完整第三方下载包。

## 已采用来源

| 来源 | 作者 | 本卧室采用范围 |
| --- | --- | --- |
| [Painted Wooden Chair 01](https://polyhaven.com/a/painted_wooden_chair_01) | Kuutti Siitonen | 椅子来源模型；后续材质调整保留木本色外观 |
| [Desk Lamp Arm 01](https://polyhaven.com/a/desk_lamp_arm_01) | Kuutti Siitonen（建模 / 纹理）、Yann Kervran（绑定） | 台灯静态网格；骨架、加权法线和自动平滑已求值，排除绑定显示用网格 |
| [Throw Pillows 01](https://polyhaven.com/a/throw_pillows_01) | Serhii Khromov | 一个坐垫用作椅垫；不用第二个坐垫，不替换床上的睡枕 |
| [Fern 02](https://polyhaven.com/a/fern_02) | Rico Cilliers（建模）、Rob Tuytel（扫描） | 选定蕨类网格与 PBR / 透明纹理，季节性布局与子集调整 |
| [Flower Gazania](https://polyhaven.com/a/flower_gazania) | James Ray Cock（建模）、Jenelle van Heerden（摄影） | 选定原生 LOD 花叶网格与纹理，季节性子集调整 |
| [Periwinkle Plant](https://polyhaven.com/a/periwinkle_plant) | Amal Kumar | 选定原生 LOD 植株与纹理，季节性子集调整 |
| [Potted Plant 02](https://polyhaven.com/a/potted_plant_02) | Rico Cilliers | 仅扫描陶盆与土壤及相关纹理；排除热带叶片 |
| [CG Japanese Maple Pack / Acer palmatum 001](https://mantissa.xyz/free.html) | Midge “Mantissa” Sinnaeve，mantissa.xyz | 院内日本枫树；树干、根、枝分材质可编辑，季节叶形 / 色素 / 雪和尺寸经过调整 |

Japanese maple model and photographed bark/leaf maps by **Midge “Mantissa” Sinnaeve, mantissa.xyz**. Mantissa 随包 `ID List.txt` 提出使用及分发时署名，原文一并保留在 [Mantissa-ID-List.txt](Mantissa-ID-List.txt)。下载包 SHA256 是本地完整性记录，不是作者签名。

## 许可与公开分发范围

上述七个 Poly Haven 来源在生产记录中标记为 **CC0-1.0**，官方许可来源为 [Poly Haven license](https://polyhaven.com/license)。两组原始本地许可文本分别逐字节保留为 [PolyHaven-Interior-CC0-1.0.txt](PolyHaven-Interior-CC0-1.0.txt) 和 [PolyHaven-Plants-CC0-1.0.txt](PolyHaven-Plants-CC0-1.0.txt)。Mantissa 来源随包的 CC0 原文保留为 [Mantissa-LICENSE-CC0.txt](Mantissa-LICENSE-CC0.txt)。

本次 Git 提交只包含卧室实际运行依赖中的派生模型 / 纹理及这些许可记录；没有将完整下载缓存当作交付附件。依据各自原许可保留这些已采用 CC0 部分的可编辑派生数据，**不把该范围扩展到整个游戏、用户参考图或其他商店资产**。原文中的专利、商标、第三方权利及担保限制仍然适用；不表示作者为本项目背书。

Unity Asset Store 的 EULA 与这些 CC0 来源是不同的许可范围。研究过但未实际导入的衣物资产包、付费床 / 收音机候选及 Yughues 资产不列为本场景已采用内容，也不附带宣传图或声称其源文件可公开分发。后续如引入商店资产，应另行记录实际购买 / 领取、许可和可公开分发边界。

## 清单与追溯

- [source-manifest.json](source-manifest.json)：从两组 Poly Haven 原清单精简而来的七项来源、作者、实际选用部分，以及原下载模型 / 纹理 URL、大小和哈希。它记录下载源，不能当作当前 Unity 派生文件的哈希清单。
- [mantissa-provenance.json](mantissa-provenance.json)：从实际 Mantissa 来源记录保留的作者、下载地址、源 ZIP / FBX SHA256、修改说明。
- [record-integrity.json](record-integrity.json)：本次复制许可文本和作为依据的原始清单的 SHA256，便于核对来源。其作用是完整性追溯，不是新增许可或数字签名。

本目录不包含原始 `.blend` / `.FBX` / `.npz`、未采用的热带叶纹理或下载 ZIP；运行所需派生资产位于 `Assets/Art/Environments/House/Bedroom/V17/`。
