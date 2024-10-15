
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generate
{
    public class VariableData
    {
        public static void TAX_RATES(DefaultVariablesData varsObj, Variables vars, DIFFICULTY diff)
        {
            TaxRates taxRates = new();

            Vector2 INCOME_TAX = varsObj.DEFAULT_TAX_RATES.Find((t) => t.DIFFICULTY == diff).INCOME_TAX;
            Vector2 VAT = varsObj.DEFAULT_TAX_RATES.Find((t) => t.DIFFICULTY == diff).VAT;
            Vector2 CORPORATE = varsObj.DEFAULT_TAX_RATES.Find((t) => t.DIFFICULTY == diff).CORPORATE_TAX;

            taxRates.INCOME_TAX = Utils.Randomize((int)INCOME_TAX.x, (int)INCOME_TAX.y);
            taxRates.VAT = Utils.Randomize((int)VAT.x, (int)VAT.y);
            taxRates.CORPORATE_TAX = Utils.Randomize((int)CORPORATE.x, (int)CORPORATE.y);

            vars.TaxRates = taxRates;
        }

        public static void ELECTRICITY_PRICE(DefaultVariablesData varsObj, Variables vars)
        {
            float price = varsObj.DEFAULT_ELECTRICITY_PRICE_PER_MWH;
            float priceMin = price + (price * 10 / 100);
            float priceMax = price - (price * 10 / 100);

            float newPrice = (float)Math.Round(Utils.RandomizeFloat(priceMin, priceMax), 2);

            vars.ELECTRICITY_PRICE_PER_MWH = newPrice;
        }

        public static void DEFAULT_SALE_RATES(DefaultVariablesData varsObj, Variables vars)
        {
            List<int> rates = Utils.GenerateRandomNumbersInRange(new List<Vector2>()
             {
                 varsObj.DEFAULT_SALE_RANGE,
                 varsObj.DEFAULT_SALE_RANGE_WITH_ADVERTISING,
                 varsObj.DEFAULT_SALE_RANGE_RECOGNITION
             });

            vars.DEFAULT_SALE_RATE = rates[0];
            vars.DEFAULT_SALE_RATE_WITH_ADVERTISING = rates[1];
            vars.DEFAULT_SALE_RATE_RECOGNITION = rates[2];
        }
    }

    public class Game
    {
        public static void BUILDINGS(GameData DATA, DefaultPrefabData PrefabData, int playerID)
        {
            List<FactoryData> factories = DATA.FACTORIES;

            List<BuildingType> toGenerate = new(){
                BuildingType.Factory,
                BuildingType.Office,
                BuildingType.Warehouse,
                BuildingType.ResearchDevelopment,
            };

            foreach (FactoryData factory in factories)
            {
                if (factory.ID == playerID)
                {
                    continue;
                }

                foreach (BuildingType type in toGenerate)
                {
                    BuildingSpawnData spawnData = PrefabData.BUILDING_SPAWN_DATA.Find((d) => d.TYPE == type);
                    BuildingPrefabData prefabData = PrefabData.BUILDINGS.Find((d) => d.TYPE == type);

                    int rate = Utils.Randomize(spawnData.INITIALIZE_CHANCE, 11);

                    if (rate > spawnData.INITIALIZE_CHANCE)
                    {
                        continue;
                    }

                    int randomPrefab = Utils.Randomize(0, prefabData.PREFABS.Count);
                    int randomGrid = Utils.Randomize(0, 4);

                    while (factory.BUILDINGS.Any((b) => b.GRID == randomGrid))
                    {
                        randomGrid = Utils.Randomize(0, 4);
                    }

                    Vector3 rotation = PrefabData.ROTATIONS[randomGrid].ROTATIONS[Utils.Randomize(0, PrefabData.ROTATIONS[randomGrid].ROTATIONS.Count())];

                    factory.BUILDINGS.Add(new BuildingData()
                    {
                        ROTATION = rotation,
                        GRID = randomGrid,
                        PREFAB = randomPrefab,
                        TYPE = type,
                    });
                }
            }
        }

        public static void RATING_LIST(GameData DATA)
        {
            List<ListRating> ratingList = new();

            List<FactoryData> factories = DATA.FACTORIES;

            foreach (FactoryData factory in factories)
            {
                ratingList.Add(new ListRating()
                {
                    FACTORY = factory.ID,
                    COMPANY = factory.COMPANY_NAME,
                    RATING = Utils.GetAverageRating(factory),
                    CASH = factory.CASH,
                    DEBT_RATIO = Utils.GetDebtCashRatio(factory),
                    SALES = Utils.IndexValue_INT(factory.DATABASE.SALES, -1),
                });
            }

            ratingList = ratingList
                .OrderBy(lr => lr.RATING)      // Higher Rating (0 is AAA)
                .ThenByDescending(lr => lr.CASH)  // Higher Cash
                .ThenBy(lr => lr.DEBT_RATIO)  // Lower Debt ratio
                .ThenByDescending(lr => lr.SALES) // Higher Sales
                .ToList();

            for (int i = 0; i < ratingList.Count; i++)
            {
                ratingList[i].ORDER = i + 1;
            }

            DATA.RATINGS = ratingList;
        }

        public static void WEATHER(GameData DATA, DefaultGameData DEFAULT_DATA)
        {
            DefaultWeatherData defaultWeatherData = DEFAULT_DATA.WEATHER.Find((w) => w.MAP == DATA.MAP_NAME);

            List<int> list = defaultWeatherData.CHANGES.Select((c) => c.CHANCE).ToList();

            int random = Utils.ChooseRandom(list);

            WeatherChangeAmount selected = defaultWeatherData.CHANGES[random];

            DATA.WEATHER.CHANGES.Add(new WeatherAnomalyData()
            {
                ANOMALY = selected.ANOMALY,
                TEMPERATURE_CHANGE = selected.CHANGE_AMOUNT,
            });

            DATA.WEATHER.SCALE = TemperatureScale.Celsius;

            SeasonalCondition season = DEFAULT_DATA.WEATHER_CONDITIONS.Find((c) => c.SEASON == Utils.GetSeason(DATA.TIME.month));
            List<int> chances = season.CONDITIONS.Select((c) => c.CHANCE).ToList();

            int randomCondition = Utils.ChooseRandom(chances);

            ConditionChance condition = season.CONDITIONS[randomCondition];

            DATA.WEATHER.CONDITION = condition.CONDITION;

            float anomalyChange = DATA.WEATHER.CHANGES[^1].TEMPERATURE_CHANGE;

            Vector2 range = defaultWeatherData.TEMPERATURES[DATA.TIME.month - 1];

            float offset = (range.y - range.x) / 2;

            float newTemperature = Utils.RandomizeFloat(range.y - offset + anomalyChange, range.y + anomalyChange);

            if (defaultWeatherData.SCALE != DATA.WEATHER.SCALE)
            {
                newTemperature = Utils.ConvertTemperatureScale(DATA.WEATHER.SCALE, newTemperature);
            }

            DATA.WEATHER.TEMPERATURES.Add((float)Math.Round(newTemperature, 1));
        }

        public static List<SalaryInfo> SALARIES(List<JobData> jobs)
        {
            List<SalaryInfo> salaries = new();

            foreach (JobData job in jobs)
            {
                SalaryInfo newGroup = new()
                {
                    JOB = job.JOB,
                    BUILDING = job.TYPE,
                    SALARY = job.INITIAL_SALARY + (job.INITIAL_SALARY * Utils.Randomize(-25, 25) / 100),
                };

                salaries.Add(newGroup);
            }

            return salaries;
        }

        public static void SUPPLIES(DefaultSectorsData defaultSectors, GameData DATA)
        {
            List<Supply> supplies = new();

            foreach (SectorData sector in DATA.SECTORS)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    foreach (InputData input in industry.INPUTS)
                    {
                        Supply supply = new()
                        {
                            NAME = input.NAME,
                            PRICE = new List<int>() { input.PRICE },
                            LICENCE_ONLY = input.LICENCE_ONLY,
                            LICENCE_FEE = input.LICENCE_FEE,
                            TYPE = input.TYPE,
                        };

                        supplies.Add(supply);
                    }
                }
            }

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                foreach (ComponentResearchDevelopment rd in factory.RESEARCH_DEVELOPMENTS)
                {
                    Supply supply = new()
                    {
                        NAME = rd.COMPONENT,
                        PRICE = new List<int>() { 0 },
                        LICENCE_ONLY = true,
                        LICENCE_FEE = 0,
                        PRODUCER_ID = factory.ID,
                        TYPE = SupplyType.ThirdParty,
                    };

                    supplies.Add(supply);
                }
            }

            DATA.SUPPLIES = supplies;
        }

        public static void BANKS(GameData DATA, DefaultFinanceData DEFAULT)
        {
            List<string> names = Utils.GetRandomBankNames(DEFAULT.NAMES, DEFAULT.BANK_COUNT);
            List<int> icons = Utils.GetRandomIntegers(8, DEFAULT.BANK_COUNT);
            List<BankAttitudeData> attitudes = new(DEFAULT.ATTITUDES);

            Vector2 range = DEFAULT.INITIAL_INTEREST_RANGES.Find((x) => x.DIFFICULTY == DATA.DIFFICULTY).RANGE;

            float rate = Utils.RandomizeFloat(range.x, range.y);

            DATA.FINANCE.BASE_INTEREST = rate;

            for (int i = 0; i < DEFAULT.BANK_COUNT; i++)
            {
                int selectedAttitude = Utils.Randomize(0, attitudes.Count);

                Bank newBank = new()
                {
                    NAME = names[i],
                    ICON = icons[i],
                    ATTITUDE = attitudes[selectedAttitude].ATTITUDE,
                    INTEREST = (float)Math.Round(Utils.ChangeByPercent(rate, attitudes[selectedAttitude].INTEREST_CHANGE), 2),
                };

                attitudes = attitudes.Where((a) => a.ATTITUDE != attitudes[selectedAttitude].ATTITUDE).ToList();

                if (i == 0)
                {
                    newBank.TERMS = DEFAULT.TERMS.Find((t) => t.SIZE == Size.Small);
                    newBank.LOANS = DEFAULT.LOANS.Find((t) => t.SIZE == Size.Small);
                    newBank.LOAN_AMOUNT_FOR_PRIVILEGE = Utils.Randomize(1, 3) * newBank.LOANS.AMOUNT[0];
                }
                else if (i == 1)
                {
                    newBank.TERMS = DEFAULT.TERMS.Find((t) => t.SIZE == Size.Medium);
                    newBank.LOANS = DEFAULT.LOANS.Find((t) => t.SIZE == Size.Medium);
                    newBank.LOAN_AMOUNT_FOR_PRIVILEGE = Utils.Randomize(2, 4) * newBank.LOANS.AMOUNT[0];
                }
                else if (i == 2)
                {
                    newBank.TERMS = DEFAULT.TERMS.Find((t) => t.SIZE == Size.Large);
                    newBank.LOANS = DEFAULT.LOANS.Find((t) => t.SIZE == Size.Large);
                    newBank.LOAN_AMOUNT_FOR_PRIVILEGE = Utils.Randomize(3, 6) * newBank.LOANS.AMOUNT[0];
                }
                else
                {
                    int randomTerm = Utils.Randomize(0, DEFAULT.TERMS.Count);
                    int randomLoan = Utils.Randomize(0, DEFAULT.LOANS.Count);
                    newBank.TERMS = DEFAULT.TERMS[randomTerm];
                    newBank.LOANS = DEFAULT.LOANS[randomLoan];
                }


                newBank.OVERDUE_INTEREST_PERCENTAGE = DEFAULT.OVERDUE_INTERESTS.Find((i) => i.SIZE == newBank.LOANS.SIZE).PERCENTAGE_OF_INTEREST;

                DATA.FINANCE.BANKS.Add(newBank);
            }
        }

        public static void MACHINES(GameData DATA, List<DefaultSectorData> sectors)
        {
            foreach (SectorData sector in DATA.SECTORS)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    DefaultSectorData defaultSector = sectors.Find((s) => s.NAME == sector.NAME);
                    INDUSTRY defaultIndustry = defaultSector.INDUSTRIES.Find((i) => i.NAME == industry.NAME);
                    List<string> names = defaultIndustry.MACHINE_NAMES.ToList();

                    int random1 = Utils.Randomize(0, names.Count);
                    string nameFirst = names[random1];
                    names.RemoveAt(random1);

                    int random2 = Utils.Randomize(0, names.Count);
                    string nameSecond = names[random2];
                    names.RemoveAt(random2);

                    industry.MACHINES = new()
                    {
                        new MachineType(){
                            NAME = nameFirst,
                            AUTOMATION = MACHINE_AUTOMATIONS.Fully,
                            PRICE = 475000,
                            RENT_PRICE = 8300,
                            ELECTRICITY_CONSUMPTION = 15
                        },
                        new MachineType(){
                            NAME = nameSecond,
                            AUTOMATION = MACHINE_AUTOMATIONS.Partially,
                            PRICE = 110000,
                            RENT_PRICE = 1900,
                            ELECTRICITY_CONSUMPTION = 8
                        }
                    };
                }
            }
        }

        public static void UpdateSupplyDemand(GameData DATA)
        {
            List<FactoryData> factories = DATA.FACTORIES;
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            List<SectorData> sectors = DATA.SECTORS;

            foreach (SectorData sector in sectors)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    float demand = factories.Where((f) => f.INDUSTRY == industry.NAME).Select((f) => f.DEMAND).Sum();
                    float supply = factories.Where((f) => f.INDUSTRY == industry.NAME).Select((f) => Utils.IndexValue(f.DATABASE.PRODUCTION, -1)).Sum();

                    industry.SUPPLY_DEMAND.Add(new(supply, demand));
                }
            }
        }

        public static List<SectorData> SECTORS(DefaultSectorsData default_sectors)
        {
            List<SectorData> newSectors = new();

            foreach (DefaultSectorData sector in default_sectors.SECTORS)
            {
                // Create SectorData from default sector data
                SectorData newSector = new()
                {
                    NAME = sector.NAME,
                    UNIT = sector.Unit.ToString(),
                    INDUSTRIES = new()
                };

                // Create IndustryData from default industry data
                foreach (INDUSTRY industry in sector.INDUSTRIES)
                {
                    int randomMaxProd = Utils.Randomize(-10, 10);

                    int max_prod = (int)Utils.ChangeByPercent(industry.PRODUCTION, randomMaxProd);

                    IndustryData industryData = new()
                    {
                        NAME = industry.NAME,
                        FEATURES = industry.FEATURES,
                        INITIAL_PRICE = industry.INITIAL_PRICE,
                        INITIAL_MAX_PRODUCTION_PER_FACTORY = max_prod,
                        AUTOMATION_RATE = Utils.ChangeByPercent(industry.AUTOMATION_RATE, Utils.Randomize(-10, 10)),
                        INPUTS = new(),
                        MACHINES = new(),
                        SUPPLY_DEMAND = new(),
                    };

                    List<Vector2> percentages = Utils.GetPercentages(industry.INPUTS);
                    List<int> currentPercentages = Utils.GenerateRandomNumbersInRange(percentages);

                    // Set input cost percentage value
                    for (int i = 0; i < industry.INPUTS.Count; i++)
                    {
                        INPUT input = industry.INPUTS[i];

                        InputData inputData = new()
                        {
                            NAME = input.NAME,
                            FEATURE = input.FEATURE,
                            AMOUNT = input.AMOUNT,
                            LICENCE_ONLY = input.LICENCE_ONLY,
                            RD_COMPLEXITY = input.RD_COMPLEXITY,
                            UNIT = input.UNIT
                        };

                        industryData.INPUTS.Add(inputData);
                    }

                    newSector.INDUSTRIES.Add(industryData);
                }

                newSectors.Add(newSector);
            }

            return newSectors;
        }

        public static List<AdvertisingData> ADVERTISING(DefaultAdvertisingData data)
        {
            List<AdvertisingData> list = new();

            List<Vector2> ranges = new();

            foreach (AdvertisingRange ad in data.ADVERTISING)
            {
                ranges.Add(ad.RANGE);
            }

            List<int> percentages = Utils.GenerateRandomNumbersInRange(ranges);

            for (int i = 0; i < percentages.Count; i++)
            {
                AdvertisingRange ad = data.ADVERTISING[i];

                int minCost = (int)Math.Round(ad.MIN - (ad.MIN * 5f / 100f));
                int maxCost = (int)Math.Round(ad.MIN + (ad.MIN * 5f / 100f));

                AdvertisingData var = new()
                {
                    NAME = ad.NAME,
                    VALUE = percentages[i],
                    MIN = Utils.Randomize(minCost, maxCost)
                };

                list.Add(var);
            }

            return list;
        }

        public static DemographyData DEMOGRAPHY(DefaultDemographyData data)
        {
            DemographyData demographyData = new();

            DevelopmentGroups[] developmentGroupCount = (DevelopmentGroups[])Enum.GetValues(typeof(DevelopmentGroups));
            DevelopmentGroups countryDevelopment = developmentGroupCount[Utils.Randomize(0, developmentGroupCount.ToList().Count)];
            demographyData.DEVELOPMENT = countryDevelopment;

            DevelopmentRanges selectedDevelopmentRange = data.PERCENTAGES.Find((d) => d.DEVELOPMENT == countryDevelopment);

            List<Vector2> educationRanges = new();
            List<Vector2> incomeRanges = new();

            foreach (EducationRanges range in selectedDevelopmentRange.EDUCATION)
            {
                educationRanges.Add(range.PERCENTAGE);
            }

            foreach (IncomeRanges range in selectedDevelopmentRange.INCOME)
            {
                incomeRanges.Add(range.PERCENTAGE);
            }

            List<float> educationPercentages = Utils.GenerateRandomFloatsInRange(educationRanges);

            for (int i = 0; i < educationPercentages.Count; i++)
            {
                EducationRanges range = selectedDevelopmentRange.EDUCATION[i];

                EducationData var = new()
                {
                    NAME = range.CATEGORY,
                    VALUE = educationPercentages[i]
                };

                demographyData.EDUCATION.Add(var);
            }

            List<float> incomePercentages = Utils.GenerateRandomFloatsInRange(incomeRanges);

            for (int i = 0; i < incomeRanges.Count; i++)
            {
                IncomeRanges range = selectedDevelopmentRange.INCOME[i];

                IncomeData var = new()
                {
                    NAME = range.CATEGORY,
                    VALUE = incomePercentages[i]
                };

                demographyData.INCOME.Add(var);
            }

            return demographyData;
        }

        public static List<Variable_INT> GetCustomizedProduct(IndustryData industry, string sectorName, List<DefaultSectorData> defaultSectors)
        {
            List<Variable_INT> list = new();

            foreach (Feature feature in industry.FEATURES)
            {
                string name = Utils.GetFeatureName(feature.NAME);

                Variable_INT newCustom = new()
                {
                    NAME = name,
                    VALUE = (int)GetComponentPercentage(defaultSectors, sectorName, industry.NAME, name).x
                };

                list.Add(newCustom);
            }

            return list;
        }

        public static List<FactoryData> FACTORIES(int count, List<SectorData> sectors, int playerID, int cash, DefaultSectorsData defaultSectors)
        {
            List<FactoryData> factories = new();

            for (int i = 0; i < count; i++)
            {
                FactoryData newFactory = new()
                {
                    ID = i,
                };

                factories.Add(newFactory);
            }

            int industryCount = Utils.GetIndustryCount(defaultSectors.SECTORS);
            int divided = count / industryCount;

            // Adds every industry to make sure every industry is included in the game
            foreach (SectorData sector in sectors)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    for (int i = 0; i < divided; i++)
                    {
                        bool isCompleted = false;

                        while (!isCompleted)
                        {
                            int randomFactory = Utils.Randomize(0, factories.Count);

                            if (randomFactory != playerID)
                            {
                                if (factories[randomFactory].SECTOR == null)
                                {
                                    FactoryData factory = factories[randomFactory];

                                    factory.SECTOR = sector.NAME;
                                    factory.INDUSTRY = industry.NAME;
                                    isCompleted = true;
                                }
                            }
                        }
                    }
                }
            }

            foreach (FactoryData factory in factories)
            {
                if (factory.ID == playerID)
                {
                    // Player Factory
                    factory.CASH = cash;
                    factory.PRODUCTION_STEP = 1;
                    factory.STORAGE_CAPACITY = 1;
                    factory.LOANS = new();
                }
                else
                {
                    // AI Factory
                    int random = Utils.Randomize(0, sectors.Count);

                    SectorData randomSector = sectors[random];
                    int randomIndustry = Utils.Randomize(0, randomSector.INDUSTRIES.Count);
                    IndustryData industry = randomSector.INDUSTRIES[randomIndustry];

                    if (factory.SECTOR == null)
                    {
                        factory.SECTOR = randomSector.NAME;
                        factory.INDUSTRY = industry.NAME;
                        factory.CUSTOMIZE_PRODUCT = GetCustomizedProduct(industry, randomSector.NAME, defaultSectors.SECTORS);
                        factory.MAX_PRODUCTION_PER_FACTORY = industry.INITIAL_MAX_PRODUCTION_PER_FACTORY;
                    }
                    else
                    {
                        SectorData selectedSector = sectors.Find((i) => i.NAME == factory.SECTOR);
                        IndustryData selectedIndustry = selectedSector.INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);

                        factory.CUSTOMIZE_PRODUCT = GetCustomizedProduct(selectedIndustry, selectedSector.NAME, defaultSectors.SECTORS);
                        factory.MAX_PRODUCTION_PER_FACTORY = industry.INITIAL_MAX_PRODUCTION_PER_FACTORY;
                    }

                    factory.LOANS = new();
                }
            }

            return factories;
        }

        public static Vector2 GetComponentPercentage(List<DefaultSectorData> sectors, string sectorName, string industryName, string featureName)
        {
            int min = 0;
            int max = 0;

            DefaultSectorData sector = sectors.Find((s) => s.NAME == sectorName);
            List<INPUT> inputs = sector.INDUSTRIES.Find((i) => i.NAME == industryName).INPUTS;
            List<INPUT> inputsFiltered = inputs.Where((i) => Utils.GetFeatureName(i.FEATURE) == featureName).ToList();

            foreach (INPUT input in inputsFiltered)
            {
                min += (int)input.COST_PERCENTAGES.x;
                max += (int)input.COST_PERCENTAGES.y;
            }

            return new Vector2(min, max);
        }

        public static List<IndustrySaleRate> FEATURES_HIGHEST_SALE_RATES(List<SectorData> sectors, DemographyData demography, List<DefaultSectorData> defaultSectors)
        {
            List<IncomeData> income = demography.INCOME.OrderByDescending((d) => d.VALUE).ToList();

            List<IndustrySaleRate> rates = new();

            foreach (SectorData sector in sectors)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    List<Vector2> ranges = new();
                    List<int> offsets = new();
                    List<FeatureTypes> preferred = Utils.GetPreferredFeatures(demography.DEVELOPMENT);

                    foreach (Feature feature in industry.FEATURES)
                    {
                        string name = Utils.GetFeatureName(feature.NAME);
                        Vector2 range = GetComponentPercentage(defaultSectors, sector.NAME, industry.NAME, name);

                        float divide = (range.y - range.x) / 5f;

                        if (range.x > 0)
                        {
                            int max;
                            int min;

                            if (preferred.Contains(feature.NAME))
                            {
                                max = (int)range.y;
                                min = (int)(range.x + divide);
                                offsets.Add((int)Math.Round(divide));
                            }
                            else
                            {
                                max = (int)(range.y - divide);
                                min = (int)range.x;
                                offsets.Add((int)Math.Round(divide));
                            }

                            ranges.Add(new Vector2(min, max));
                        }
                        else
                        {
                            ranges.Add(new Vector2(0, 0));
                            offsets.Add(0);
                        }
                    }

                    float minSum = ranges.Select((r) => r.x).Sum();
                    float maxSum = ranges.Select((r) => r.y).Sum();

                    while (minSum > 100)
                    {
                        int random = Utils.Randomize(0, ranges.Count);

                        if (offsets[random] > 0)
                        {
                            ranges[random] = new Vector2(ranges[random].x - 1, ranges[random].y);
                            offsets[random] = offsets[random] - 1;
                            minSum = ranges.Select((r) => r.x).Sum();
                        }
                    }

                    while (maxSum < 100)
                    {
                        int random = Utils.Randomize(0, ranges.Count);

                        if (offsets[random] > 0)
                        {
                            ranges[random] = new Vector2(ranges[random].x, ranges[random].y + 1);
                            offsets[random] = offsets[random] - 1;
                            maxSum = ranges.Select((r) => r.y).Sum();
                        }
                    }

                    List<int> newRanges = Utils.GenerateRandomNumbersInRange(ranges);

                    for (int i = 0; i < newRanges.Count; i++)
                    {
                        Feature feature = industry.FEATURES[i];
                        string name = Utils.GetFeatureName(feature.NAME);

                        IndustrySaleRate newRate = new()
                        {
                            NAME = name,
                            INDUSTRY = industry.NAME,
                            VALUE = newRanges[i]
                        };

                        rates.Add(newRate);
                    }
                }
            }

            return rates;
        }

        public static void SUPPLY_DEMAND(GameData DATA)
        {
            foreach (SectorData sector in DATA.SECTORS)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    industry.SUPPLY_DEMAND.Clear();

                    List<FactoryData> filteredFactories = DATA.FACTORIES.Where((f) => f.SECTOR == sector.NAME && f.INDUSTRY == industry.NAME).ToList();

                    int random = Utils.Randomize(-10, 25);

                    int supply = filteredFactories.Select((f) => AI.GetProduction(f, DATA.VARIABLES.MAX_PRODUCTION_STEP)).Sum();
                    int demand = (int)Math.Round(supply + (supply * random / 100f));

                    Vector2 Supply_Demand = new(supply, demand);

                    industry.SUPPLY_DEMAND.Add(Supply_Demand);
                }
            }
        }
    }

    public class AI
    {
        public static void INITIALIZE_CREDIT_RATINGS(GameData DATA)
        {
            foreach (FactoryData factory in DATA.FACTORIES)
            {
                foreach (Bank bank in DATA.FINANCE.BANKS)
                {
                    factory.CREDIT_RATINGS.Add(new Variable_INT()
                    {
                        NAME = bank.NAME,
                        VALUE = 0
                    });
                }
            }
        }

        public static void COMPANY_AGES(GameData DATA)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    int random = Utils.Randomize(0, 10);

                    if (random < 3)
                    {
                        factory.FACTORY_AGE_IN_MONTH = Utils.Randomize(0, 3);
                    }
                    else
                    {
                        factory.FACTORY_AGE_IN_MONTH = Utils.Randomize(3, 56);
                    }
                }
            }
        }

        public static void COMPANY_NAMES(GameData DATA, List<FactoryNamePool> POOL)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            List<FactoryNamePool> POOL_COPY = POOL.Select(p => new FactoryNamePool
            {
                INDUSTRY = p.INDUSTRY,
                NAMES = new List<string>(p.NAMES)
            }).ToList();

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (playerFactories.Contains(factory.ID))
                {
                    factory.COMPANY_NAME = DATA.COMPANY_NAME;
                }
                else
                {
                    FactoryNamePool pool = POOL_COPY.Find(p => p.INDUSTRY == factory.INDUSTRY);
                    List<string> names = pool.NAMES;

                    int random = Utils.Randomize(0, names.Count);

                    factory.COMPANY_NAME = names[random];

                    names.RemoveAt(random);
                }
            }
        }

        public static int GetNeededMachineCount(FactoryData factory, DefaultSectorsData sectors, IndustryData industry)
        {
            int operators = GetNeededEmployeeCount(factory, sectors, industry, BuildingType.Factory, StaffType.Operator, false);

            int neededMachines = (int)Math.Round(operators * industry.AUTOMATION_RATE / 100f);

            return neededMachines;
        }

        public static void MACHINES(GameData DATA, DefaultSectorsData sectors)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    SectorData sector = DATA.SECTORS.Find((s) => s.NAME == factory.SECTOR);
                    IndustryData industry = sector.INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);

                    List<MachineType> machinesFully = industry.MACHINES.Where((m) => m.AUTOMATION == MACHINE_AUTOMATIONS.Fully).ToList();
                    List<MachineType> machinesPartially = industry.MACHINES.Where((m) => m.AUTOMATION == MACHINE_AUTOMATIONS.Partially).ToList();

                    if (factory.SALE_MULTIPLIER > .7f)
                    {
                        int random = Utils.Randomize(0, 2);

                        if (random == 0)
                        {
                            factory.MACHINE_AUTOMATION = MACHINE_AUTOMATIONS.Fully;
                        }
                        else
                        {
                            factory.MACHINE_AUTOMATION = MACHINE_AUTOMATIONS.Fully;
                        }
                    }
                    else
                    {
                        int random = Utils.Randomize(0, 10);

                        if (random == 0)
                        {
                            factory.MACHINE_AUTOMATION = MACHINE_AUTOMATIONS.Fully;
                        }
                        else
                        {
                            factory.MACHINE_AUTOMATION = MACHINE_AUTOMATIONS.Partially;
                        }
                    }

                    int randomRent = Utils.Randomize(0, 10);

                    if (randomRent < 8)
                    {
                        factory.MACHINES = new(){
                        new MachineCount(){
                            AUTOMATION = factory.MACHINE_AUTOMATION,
                            NAME =machinesFully[Utils.Randomize(0,machinesFully.Count)].NAME,
                            COUNT =  GetNeededMachineCount(factory,sectors,industry),
                            RENTED = false,
                        }
                    };
                    }
                    else
                    {
                        factory.MACHINES = new(){
                        new MachineCount(){
                            AUTOMATION = factory.MACHINE_AUTOMATION,
                            NAME =machinesFully[Utils.Randomize(0,machinesFully.Count)].NAME,
                            COUNT = 0,
                            RENTED = true,
                        }
                     };
                    }
                }
                else
                {
                    factory.MACHINES = new() { };
                }
            }
        }

        public static void STORAGE_CAPACITY(GameData DATA)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {

                    switch (factory.PRODUCT_POLICY)
                    {
                        case ProductPolicy.ProfitDriven:
                            factory.STORAGE_CAPACITY = Utils.Randomize(5, 11);
                            break;
                        case ProductPolicy.Exploitative:
                        case ProductPolicy.Dominant:
                        case ProductPolicy.Ruthless:
                            factory.STORAGE_CAPACITY = Utils.Randomize(1, 5);
                            break;
                    }
                }
            }
        }

        public static int GetNeededEmployeeCount(FactoryData factory, DefaultSectorsData SECTORS, IndustryData industryData, BuildingType building = BuildingType.None, StaffType job = StaffType.None, bool addAutomation = true)
        {
            int count = 0;

            DefaultSectorData defaultSector = SECTORS.SECTORS.Find((s) => s.NAME == factory.SECTOR);
            INDUSTRY industry = defaultSector.INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);

            float automation = building == BuildingType.Factory && addAutomation ? industryData.AUTOMATION_RATE : -1;

            int step = factory.PRODUCTION_STEP;

            if (job == StaffType.None && building != BuildingType.None)
            {
                DefaultStaffData staffData = industry.STAFF.Find((s) => s.BUILDING == building);

                foreach (StaffPercentage dist in staffData.STAFF_DISTRIBUTION)
                {
                    count += Utils.CalculateNeededStaffCount(dist.PERCENTAGE, staffData.NEEDED_STAFF_COUNT, factory.PRODUCTION_STEP, automation);
                }
            }

            if (job == StaffType.None && building == BuildingType.None)
            {
                foreach (BuildingData b in factory.BUILDINGS)
                {
                    DefaultStaffData staffData = industry.STAFF.Find((s) => s.BUILDING == building);

                    foreach (StaffPercentage dist in staffData.STAFF_DISTRIBUTION)
                    {
                        count += Utils.CalculateNeededStaffCount(dist.PERCENTAGE, staffData.NEEDED_STAFF_COUNT, factory.PRODUCTION_STEP, automation);
                    }
                }
            }

            DefaultStaffData staffBuildingData = industry.STAFF.Find((s) => s.BUILDING == building);

            StaffPercentage staffDistData = staffBuildingData.STAFF_DISTRIBUTION.Find((s) => s.JOB == job);

            return Utils.CalculateNeededStaffCount(staffDistData.PERCENTAGE, staffBuildingData.NEEDED_STAFF_COUNT, factory.PRODUCTION_STEP, automation);
        }

        public static void STAFF(GameData DATA, DefaultSectorsData SECTORS)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (playerFactories.Contains(factory.ID))
                {
                    continue;
                }

                INDUSTRY defaultIndustry = SECTORS.SECTORS.Find((s) => s.NAME == factory.SECTOR).INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);

                foreach (BuildingData building in factory.BUILDINGS)
                {
                    foreach (DefaultStaffData staffData in defaultIndustry.STAFF)
                    {
                        if (staffData.BUILDING != building.TYPE)
                        {
                            continue;
                        }

                        foreach (StaffPercentage percentage in staffData.STAFF_DISTRIBUTION)
                        {
                            EmployeeCount newEmp = new()
                            {
                                JOB = percentage.JOB,
                                BUILDING = staffData.BUILDING
                            };

                            IndustryData industryData = DATA.SECTORS.Find((s) => s.NAME == factory.SECTOR).INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);

                            int needed = GetNeededEmployeeCount(factory, SECTORS, industryData, building.TYPE, percentage.JOB);

                            switch (factory.PRODUCT_POLICY)
                            {
                                case ProductPolicy.Ruthless:
                                case ProductPolicy.ProfitDriven:
                                    newEmp.COUNT = needed + (needed * Utils.Randomize(-10, 0) / 100);
                                    break;
                                case ProductPolicy.Dominant:
                                case ProductPolicy.Exploitative:
                                    newEmp.COUNT = needed + (needed * Utils.Randomize(0, 10) / 100);
                                    break;
                            }

                            factory.STAFF.Add(newEmp);
                        }
                    }
                }
            }
        }

        public static void PRODUCT_POLICY(GameData DATA)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            ProductPolicy[] policies = (ProductPolicy[])Enum.GetValues(typeof(ProductPolicy));
            List<ProductPolicy> filtered = policies.Where((p) => p != ProductPolicy.None).ToList();

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    ProductPolicy policy = policies[Utils.Randomize(0, filtered.Count)];
                    factory.PRODUCT_POLICY = policy;
                }
            }
        }

        public static int GetSupplyPrice(FactoryData factory, Supply supply, SectorData sector, List<Variable_INT> customs, int max_step)
        {
            int SCALE_DISCOUNT = 10;

            IndustryData industry = sector.INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);
            int step = factory.PRODUCTION_STEP;

            InputData input = industry.INPUTS.Find((i) => i.NAME == supply.NAME);
            Feature feature = industry.FEATURES.Find((f) => f.NAME == input.FEATURE);

            Variable_INT custom = customs.Find((c) => c.NAME == Utils.GetFeatureName(feature.NAME));

            int cost;

            if (custom.VALUE > 0)
                cost = supply.PRICE[^1] * custom.VALUE * 5 / 100;
            else
                cost = supply.PRICE[^1];

            cost *= (int)Math.Round((float)step / max_step * factory.MAX_PRODUCTION_PER_FACTORY); // Production Count
            cost *= (int)Math.Round((float)step / max_step * SCALE_DISCOUNT); // Scale Discount

            return cost;
        }

        public static List<InputData> GetInputs(FactoryData factory, SectorData sector)
        {
            IndustryData industry = sector.INDUSTRIES.Find((i) => i.NAME == factory.INDUSTRY);
            return industry.INPUTS;
        }


        public static void SUPPLIES(GameData DATA)
        {
            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (DATA.PLAYER_FACTORIES.Contains(factory.ID))
                    continue;

                SectorData sector = DATA.SECTORS.Find((s) => s.NAME == factory.SECTOR);
                List<InputData> inputs = GetInputs(factory, sector);

                foreach (InputData input in inputs)
                {
                    bool hasRD = factory.RESEARCH_DEVELOPMENTS.Any((rd) => rd.COMPONENT == input.NAME);

                    FactorySupply supply = new()
                    {
                        NAME = input.NAME,
                        PRODUCER_ID = hasRD ? factory.ID : -1,
                    };
                }
            }
        }

        public static int GetComponentCost(FactoryData factory, List<Supply> supplies, SectorData sector, int max_step)
        {
            float total = 0;

            foreach (FactorySupply factorySupply in factory.SUPPLIES)
            {
                Supply supply = supplies.Find((s) => s.NAME == factorySupply.NAME);

                total += GetSupplyPrice(factory, supply, sector, factory.CUSTOMIZE_PRODUCT, max_step);
            }

            return (int)Math.Round(total);
        }

        public static int GetStaffSalary(FactoryData factory, List<SalaryInfo> groups)
        {
            int total = 0;

            foreach (EmployeeCount e in factory.STAFF)
            {
                total += groups.Find((w) => w.JOB == e.JOB).SALARY * e.COUNT;
            }

            return total;
        }

        public static int GetStaffCost(FactoryData factory, List<SalaryInfo> groups, int max_step)
        {
            int salary = GetStaffSalary(factory, groups);

            int count = GetProduction(factory, max_step);

            return (int)Math.Round((float)salary / count);
        }

        public static int GetMarketingCost(FactoryData factory, int max_step)
        {
            int sum = factory.ADVERTISING_BUDGET.Select((ad) => ad.BUDGET).Sum();

            int production = GetProduction(factory, max_step);

            return (int)Math.Round((float)sum / production);
        }

        public static void CASH(GameData DATA)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    int cash = Utils.Randomize(10, 50) * 1000000;
                    factory.CASH = cash;
                }
            }
        }

        public static void PRODUCT_PRICE(GameData DATA)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    SectorData sector = DATA.SECTORS.Find((s) => s.NAME == factory.SECTOR);
                    int componentCost = GetComponentCost(factory, DATA.SUPPLIES, sector, DATA.VARIABLES.MAX_PRODUCTION_STEP);
                    int staffCost = GetStaffCost(factory, DATA.MARKET_SALARIES, DATA.VARIABLES.MAX_PRODUCTION_STEP);
                    int marketingCost = GetMarketingCost(factory, DATA.VARIABLES.MAX_PRODUCTION_STEP);

                    int total = componentCost + staffCost + marketingCost;

                    int min = 0;
                    int max = 0;

                    switch (factory.PRODUCT_POLICY)
                    {
                        case ProductPolicy.Ruthless:
                            min = (int)Math.Round(total * 10f / 100f);
                            max = (int)Math.Round(total * 20f / 100f);
                            break;
                        case ProductPolicy.ProfitDriven:
                            min = (int)Math.Round(total * 25f / 100f);
                            max = (int)Math.Round(total * 35f / 100f);
                            break;
                        case ProductPolicy.Dominant:
                            min = (int)Math.Round(total * 5f / 100f);
                            max = (int)Math.Round(total * 15f / 100f);
                            break;
                        case ProductPolicy.Exploitative:
                            min = (int)Math.Round(total * 20f / 100f);
                            max = (int)Math.Round(total * 30f / 100f);
                            break;
                    }

                    int price = total + Utils.Randomize(min, max);

                    factory.PRICE = (int)Math.Round(price / 100.0) * 100;
                    factory.PROFIT_MARGIN = (float)Math.Round((float)(factory.PRICE - total) / total * 100f, 2);
                }
            }
        }

        public static void ADVERTISING_BUDGET(GameData data)
        {
            List<int> playerFactories = data.PLAYER_FACTORIES;

            foreach (FactoryData factory in data.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    foreach (AdvertisingData ad in data.ADVERTISING)
                    {
                        factory.ADVERTISING_BUDGET.Add(new()
                        {
                            NAME = ad.NAME,
                            BUDGET = (int)Math.Round((float)Utils.Randomize(ad.MIN, (int)Math.Round(ad.MIN * (factory.SALE_MULTIPLIER * 6)))),
                        });
                    }
                }
            }

        }

        public static void RECOGNITION(GameData data)
        {
            foreach (FactoryData factory in data.FACTORIES)
            {
                if (factory.SALE_MULTIPLIER > .7f)
                {
                    if (Utils.Randomize(0, 10) == 0)
                    {
                        factory.RECOGNITION = Utils.RandomizeFloat(.1f, 5f);
                    }
                    else
                    {
                        factory.RECOGNITION = Utils.RandomizeFloat(.7f, 1f);
                    }
                }
                else if (factory.SALE_MULTIPLIER > .3f)
                {
                    factory.RECOGNITION = Utils.RandomizeFloat(.3f, .7f);
                }
                else
                {
                    if (Utils.Randomize(0, 10) == 0)
                    {
                        factory.RECOGNITION = Utils.RandomizeFloat(.7f, 1f);
                    }
                    else
                    {
                        factory.RECOGNITION = Utils.RandomizeFloat(0, .3f);
                    }
                }
            }
        }

        public static int GetCapacity(int step, int max_step)
        {
            return (int)Math.Round(step * 100f / max_step);
        }

        public static float GetAdvertisingMultiplier(List<AdvertisingBudget> budgets, List<AdvertisingData> ads)
        {
            float multiplier = 0f;

            foreach (AdvertisingData ad in ads)
            {
                float budget = budgets.Find((b) => b.NAME == ad.NAME).BUDGET;

                float midPoint = ad.MIN * 2f;
                float adMultiplier = ad.VALUE / 100f;

                multiplier += Utils.Sigmoid(budget, adMultiplier, midPoint);
            }

            return multiplier;
        }

        public static int GetProduction(FactoryData factory, int max_step, bool _base = false)
        {
            float multiplier = 1.25f;

            int capacity = GetCapacity(factory.PRODUCTION_STEP == 0 ? max_step : factory.PRODUCTION_STEP, max_step);

            int maxProduction = (int)Math.Ceiling(factory.MAX_PRODUCTION_PER_FACTORY * multiplier);
            int production = (int)Math.Ceiling(maxProduction * capacity / 100f);

            if (_base)
            {
                return (int)Math.Ceiling(factory.MAX_PRODUCTION_PER_FACTORY * capacity / 100f);
            }

            return production;
        }

        public static int PRODUCT_SALE(FactoryData factory, Variables vars, List<AdvertisingData> ads)
        {
            int demand = factory.DEMAND;

            int defaultSaleRate = vars.DEFAULT_SALE_RATE_RECOGNITION;
            int defaultAdSaleRate = vars.DEFAULT_SALE_RATE_WITH_ADVERTISING;
            int defaultRecognitionSaleRate = vars.DEFAULT_SALE_RATE_RECOGNITION;

            int defaultDemand = (int)Math.Round(demand * defaultSaleRate / 100f);
            int advertisingDemand = (int)Math.Round(GetAdvertisingMultiplier(factory.ADVERTISING_BUDGET, ads) * (demand * defaultAdSaleRate / 100f));
            int recognitionDemand = (int)Math.Round(factory.RECOGNITION * (demand * defaultRecognitionSaleRate / 100f));

            int total = defaultDemand + advertisingDemand + recognitionDemand;

            int randomness = Utils.Randomize(-5, 5);

            int final = (int)Math.Round(total + (total * randomness / 100f));

            return final;
        }

        public static void INITIAL_SALES(GameData DATA)
        {
            foreach (SectorData sector in DATA.SECTORS)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    List<FactoryData> filtered = DATA.FACTORIES.Where((f) => f.INDUSTRY == industry.NAME).ToList();
                    Vector2 Supply_Demand = industry.SUPPLY_DEMAND[^1];

                    float diff = (Supply_Demand.y - Supply_Demand.x) / Supply_Demand.x;

                    foreach (FactoryData factory in filtered)
                    {
                        if (factory.SALE_MULTIPLIER == 0)
                        {
                            continue;
                        }

                        int sale = PRODUCT_SALE(factory, DATA.VARIABLES, DATA.ADVERTISING);
                        int productCount = (int)Math.Floor(factory.PRODUCT_COUNT);

                        if (Supply_Demand.x < Supply_Demand.y)
                        {
                            if (factory.PRODUCT_COUNT <= sale)
                            {
                                factory.SALES.Add(productCount);
                                factory.DATABASE.SALES.Add(productCount);
                                factory.PRODUCT_COUNT -= productCount;
                            }
                            else
                            {
                                factory.PRODUCT_COUNT -= sale;
                                factory.SALES.Add(sale);
                                factory.DATABASE.SALES.Add(sale);
                            }
                        }
                        else
                        {
                            int newSale = (int)Math.Round(sale + sale * diff);

                            if (factory.PRODUCT_COUNT <= newSale)
                            {
                                factory.SALES.Add(productCount);
                                factory.DATABASE.SALES.Add(productCount);
                                factory.PRODUCT_COUNT -= productCount;
                            }
                            else
                            {
                                factory.PRODUCT_COUNT -= newSale;
                                factory.SALES.Add(newSale);
                                factory.DATABASE.SALES.Add(newSale);
                            }
                        }
                    }
                }
            }
        }

        public static void INITIAL_PRODUCTION(GameData DATA)
        {
            List<int> playerFactories = DATA.PLAYER_FACTORIES;

            foreach (FactoryData factory in DATA.FACTORIES)
            {
                if (!playerFactories.Contains(factory.ID))
                {
                    int amount = factory.MAX_PRODUCTION_PER_FACTORY;

                    factory.PRODUCT_COUNT += amount;
                    factory.DATABASE.PRODUCTION.Add(amount);
                }
            }
        }

        public static void PRODUCTION_STEP(GameData DATA)
        {
            foreach (FactoryData factory in DATA.FACTORIES)
            {
                int prod = factory.MAX_PRODUCTION_PER_FACTORY;
                int demand = (int)Math.Round(factory.SALE_MULTIPLIER * prod);

                float per = prod / DATA.VARIABLES.MAX_PRODUCTION_STEP;

                for (int i = 1; i <= DATA.VARIABLES.MAX_PRODUCTION_STEP; i++)
                {
                    if (i * per > demand)
                    {
                        factory.PRODUCTION_STEP = i;
                        factory.DEMAND = demand;
                        break;
                    }
                }
            }
        }

        public static List<Variable_INT> GenerateRandomFeatureValues(List<DefaultSectorData> sectors, List<Variable_INT> features, string sectorName, string industryName, List<IndustrySaleRate> rates, int r)
        {
            List<Variable_INT> newFeatures = new();

            foreach (Variable_INT feature in features)
            {
                Vector2 range = Game.GetComponentPercentage(sectors, sectorName, industryName, feature.NAME);

                int value = rates.Find((s) => s.NAME == feature.NAME).VALUE;
                int offset = (int)range.y - (int)range.x;
                float point = 1f / offset;

                Variable_INT newFeature = new()
                {
                    NAME = feature.NAME,
                    VALUE = 1
                };

                if (r == 0)
                {
                    int count = (int)Math.Round(offset * 0.2f);
                    int random = Utils.Randomize(value - count, value + count);
                    newFeature.VALUE = (int)Math.Clamp(random, range.x, range.y);
                }
                else if (r == 1)
                {
                    int least = (int)Math.Round(offset * 0.5f);
                    int max = (int)Math.Round(offset * 0.75f);
                    int min = (int)Math.Round(offset * 0.75f);
                    int randomMin = Utils.Randomize(value - min, value - least);
                    int randomMax = Utils.Randomize(value + least, value + max);

                    int coin = Utils.Randomize(0, 2);

                    if (coin == 0)
                    {
                        newFeature.VALUE = (int)Math.Clamp(randomMin, range.x, range.y);
                    }
                    else
                    {
                        newFeature.VALUE = (int)Math.Clamp(randomMax, range.x, range.y);
                    }
                }
                else
                {
                    int least = (int)Math.Round(offset * 0.85f);
                    int max = (int)Math.Round(offset * 0.99f);
                    int min = (int)Math.Round(offset * 0.99f);
                    int randomMin = Utils.Randomize(value - min, value - least);
                    int randomMax = Utils.Randomize(value + least, value + max);

                    int coin = Utils.Randomize(0, 2);

                    if (coin == 0)
                    {
                        newFeature.VALUE = (int)Math.Clamp(randomMin, range.x, range.y);
                    }
                    else
                    {
                        newFeature.VALUE = (int)Math.Clamp(randomMax, range.x, range.y);
                    }
                }

                newFeatures.Add(newFeature);
            }

            return newFeatures;
        }

        public static float GetSaleMultiplier(List<DefaultSectorData> sectors, List<Variable_INT> features, string industryName, string sectorName, List<IndustrySaleRate> sale_rates = null)
        {
            float multiplier = 0;

            // Specific sale rates for the features.
            //E.G.: Automotive => Performance(12,46) Value == 33. That means 33 is the best sale rate for that feature
            List<IndustrySaleRate> rates = sale_rates;

            if (sale_rates == null)
            {
                rates = GameManager.instance.DATA.SALE_RATES;
            }

            foreach (Variable_INT var in features)
            {
                IndustrySaleRate rate = rates.Find((s) => s.NAME == var.NAME && s.INDUSTRY == industryName);

                Vector2 range = Game.GetComponentPercentage(sectors, sectorName, industryName, var.NAME);
                int diff = Math.Abs(rate.VALUE - var.VALUE);
                int offset = (int)range.y - (int)range.x;
                float point = 1f / offset;

                if (diff != 0 && offset > 0)
                {
                    multiplier += 1f - (diff * point);
                }
            }

            return multiplier / features.Count;
        }

        public static void SALE_MULTIPLIER(GameData DATA, List<DefaultSectorData> sectors)
        {
            List<FactoryData> filteredFactories = DATA.FACTORIES.Where((f) => f.ID != DATA.PLAYER_FACTORIES[0]).ToList();

            foreach (SectorData sector in DATA.SECTORS)
            {
                foreach (IndustryData industry in sector.INDUSTRIES)
                {
                    List<FactoryData> industryFactories = filteredFactories.Where((f) => f.INDUSTRY == industry.NAME && !DATA.PLAYER_FACTORIES.Contains(f.ID)).ToList();
                    int count = industryFactories.Count;
                    int sumCount = 0;
                    List<int> profitDistribution = Utils.GetProfitDistribution();

                    for (int r = 0; r < profitDistribution.Count; r++)
                    {
                        int rate = profitDistribution[r];
                        int selectedCount = count * rate / 100;

                        sumCount += selectedCount;

                        if (r == profitDistribution.Count - 1)
                        {
                            if (sumCount < count)
                            {
                                selectedCount += count - sumCount;
                            }
                        }

                        for (int i = 0; i < selectedCount; i++)
                        {
                            FactoryData selectedFactory = industryFactories[i];

                            List<Variable_INT> temp = GenerateRandomFeatureValues(sectors, selectedFactory.CUSTOMIZE_PRODUCT, sector.NAME, industry.NAME, DATA.SALE_RATES, r);
                            float multiplier = GetSaleMultiplier(sectors, temp, industry.NAME, sector.NAME, DATA.SALE_RATES);

                            FactoryData foundFactory = DATA.FACTORIES.Find((f) => f.ID == selectedFactory.ID);
                            foundFactory.CUSTOMIZE_PRODUCT = temp;
                            foundFactory.SALE_MULTIPLIER = multiplier;
                        }

                        industryFactories.RemoveRange(0, selectedCount);
                    }
                }
            }
        }
    }
}