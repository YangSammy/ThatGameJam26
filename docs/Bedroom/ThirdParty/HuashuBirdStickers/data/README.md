# Data

`species.json` is the complete dataset, one record per species, sorted in IOC sequence. The CSV files are flattened subsets for spreadsheets.

| File | Contents | License |
|---|---|---|
| `species.json` | Everything below except the Chinese field-guide text | MIT for our compilation; third-party fields keep their terms (see Sources) |
| `species.csv` | Taxonomy, main names, IDs, sticker and reference info | as above |
| `names.csv` | Names in 43 languages, one column per language | CC BY 3.0 (IOC) |
| `languages.json` | Language codes and the IOC column each comes from | MIT |
| `field-guide.zh-Hans.json` | Chinese field-guide notes for 1,596 species | CC BY-SA 4.0 |

## Fields in `species.json`

| Field | What it is | Source | Coverage |
|---|---|---|---|
| `id` | `Genus_species`, stable key used for file names | — | all |
| `scientificName`, `authority` | | IOC 15.2 | all |
| `taxonomy` | IOC sequence, order, family, family English name, genus | IOC 15.2 | all |
| `names` | Common names keyed by language code | IOC 15.2 multilingual | see main README |
| `ids` | Wikidata, eBird, GBIF, iNaturalist IDs | Wikidata; iNaturalist | 10,939 have Wikidata |
| `range` | IOC breeding region code, breeding range, nonbreeding range; `zh-Hans` is a Chinese summary | IOC 15.2 | all |
| `subspecies` | Subspecies with authority and range | IOC 15.2 | species with subspecies |
| `traits` | Mass (g), wing, tail, tarsus, bill measurements (mm), hand-wing index, habitat, migration, trophic level and niche, primary lifestyle. `inferred: true` means AVONET imputed some traits from relatives | AVONET | 10,577 |
| `size` | Body length and wingspan in cm, as `[min, max]`, with where the length came from | Wikidata, Wikipedia text | about 4,600 |
| `conservation` | IUCN Red List category, CITES appendix, China's national protection class | Wikidata P141 / P7603; China's 2021 list of protected wild animals | IUCN 10,180 |
| `zh` | Pinyin, Chinese aliases, Chinese order and family names, Chinese Wikipedia page | Wikidata, Chinese Wikipedia | 11,041 (not the 23 recent splits that have no Chinese name yet) |
| `sticker` | File, kind, review score, whether it meets the quality bar, license, and the reference it was drawn from | this project | all |

Notes:

- `conservation.iucn` is taken from Wikidata and may lag behind the current Red List. Check <https://www.iucnredlist.org/> for the latest assessment.
- `zh.aliases` come from Wikidata and Chinese Wikipedia and have not been checked one by one; a few are names of other, similar species. Corrections welcome.
- Not included: per-province records and monthly occurrence derived from eBird data (we have not confirmed the terms for redistributing them), and Wikipedia article text (link only).

## Reference photos

Each AI sticker was drawn from one reference photo, or from a board of several images. Its page, author and license are in `sticker.reference`; the photos themselves are not distributed. By species:

| Reference | Species |
|---|---|
| Openly licensed photo (CC0, public domain, CC BY, CC BY-SA) | 2,020 |
| Board of openly licensed images, or a written description only | 46 |
| Photo under a non-commercial or no-derivatives license (CC BY-NC, -ND, …) | 5,071 |
| Photo with all rights reserved, or under Macaulay Library terms | 1,500 |
| Photo whose license we did not record | 2,017 |
| Board mixing images under different terms | 410 |

The stickers are new drawings, not copies. Whether a drawing made from a photo is bound by that photo's license is not settled law; if it matters for your use, filter on `sticker.reference.license`.

## Sources

- IOC World Bird List v15.2, Gill, Donsker & Rasmussen (Eds), [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/). <https://worldbirdnames.org/>
- AVONET: Tobias, J. A. et al. (2022). AVONET: morphological, ecological and geographical data for all birds. *Ecology Letters* 25: 581–597. doi:[10.1111/ele.13898](https://doi.org/10.1111/ele.13898). [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
- Wikidata, [CC0](https://creativecommons.org/publicdomain/zero/1.0/).
- 国家重点保护野生动物名录 (2021), National Forestry and Grassland Administration and Ministry of Agriculture and Rural Affairs of China.
- Chinese field-guide notes are rewritten from Chinese Wikipedia and other sources and are shared under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/); `basedOn` gives the main page used.
