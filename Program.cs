using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Masters;

namespace SynBanditWarForwarder
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }
        public static bool ItemEquivalence(IReadOnlyList<IContainerEntryGetter>? item1, IReadOnlyList<IContainerEntryGetter>? item2)
        {
            bool itemEquivalence = !(item1 is null ^ item2 is null);
            if(item1 is not null && item2 is not null)
            {
                foreach(var item in item1)
                {
                    if(!(item2.Contains(item)))
                    {
                        itemEquivalence = false;
                    }
                }
                foreach(var item in item2)
                {
                    if(!(item1.Contains(item)))
                    {
                        itemEquivalence = false;
                    }
                }
            }
            return itemEquivalence;
        }
        public static bool PerkEquivalence(IReadOnlyList<IPerkPlacementGetter>? item1, IReadOnlyList<IPerkPlacementGetter>? item2)
        {
            bool perkEquivalence = !(item1 is null ^ item2 is null);
            if(item1 is not null && item2 is not null)
            {
                foreach(var perk in item1)
                {
                    if(!(item2.Contains(perk)))
                    {
                        perkEquivalence = false;
                    }
                }
                foreach(var perk in item2)
                {
                    if(!(item1.Contains(perk)))
                    {
                        perkEquivalence = false;
                    }
                }
            }
            return perkEquivalence;
        }
        //I wish I read up on masks before I wrote this
        public static bool BWEquivalence(INpcGetter mod1,INpcGetter mod2)
        {
            bool configEquivalence = mod1.Configuration.Equals(mod2.Configuration);
            bool tpltEquivalnce = mod1.Template.Equals(mod2.Template);
            bool itemEquivalence = ItemEquivalence(mod1.Items,mod2.Items);
            bool aidtEquivalence = mod1.AIData.Equals(mod2.AIData);
            bool doftEquivalence = mod1.DefaultOutfit.Equals(mod2.DefaultOutfit);
            bool nameEquivalence = !(mod1.Packages is null ^ mod2.Packages is null);
            if(mod1.Name is not null && mod2.Name is not null)
            {
                nameEquivalence = mod1.Name.Equals(mod2.Name);
            }
            bool pkgEquivalence = !(mod1.Packages is null ^ mod2.Packages is null);
            if(mod1.Packages is not null && mod2.Packages is not null)
            {
                foreach(var pkg in mod1.Packages)
                {
                    if(!(mod2.Packages.Contains(pkg)))
                    {
                        pkgEquivalence = false;
                    }
                }
            }
            bool cnamEquivalence = mod1.Class.Equals(mod2.Class);
            bool dnamEquivalence = !(mod1.PlayerSkills is null ^ mod2.PlayerSkills is null);            
            if(mod1.PlayerSkills is not null && mod2.PlayerSkills is not null)
            {
                dnamEquivalence = mod1.PlayerSkills.Equals(mod2.PlayerSkills);
            }
            bool znamEquivalence = mod1.CombatStyle.Equals(mod2.CombatStyle);
            bool dpEquivalence = mod1.DefaultPackageList.Equals(mod2.DefaultPackageList);
            bool perkEquivalence = PerkEquivalence(mod1.Perks,mod2.Perks);
            return configEquivalence&&tpltEquivalnce&&itemEquivalence&&aidtEquivalence&&pkgEquivalence&&cnamEquivalence&&dnamEquivalence&&znamEquivalence&&dpEquivalence&&nameEquivalence&&perkEquivalence&&doftEquivalence;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (!ModKey.TryFromFileName("Bandit War.esp", out var modKey))
            {
                throw new Exception("Missing Bandit War.esp");
            }
            var bweMod = state.LoadOrder[modKey].Mod;
            if(bweMod is null)
            {
                throw new Exception("Bandit War.esp cannot be read");
            }
            foreach(var bweNPC in bweMod.Npcs)
            {
                var npcRecordContextArray = bweNPC.ToLink().ResolveAllContexts<ISkyrimMod, ISkyrimModGetter, INpc, INpcGetter>(state.LinkCache).Reverse().ToArray();
                var bweWinningOverride = npcRecordContextArray[npcRecordContextArray.Count()-1].Record;
                var bweOverride = bweWinningOverride.DeepCopy();
                if(npcRecordContextArray.Count()>1&&!(BWEquivalence(bweNPC,bweWinningOverride)||npcRecordContextArray[npcRecordContextArray.Count()-1].ModKey.Name.Contains("Bandit War")))
                {
                    for(int i = 0;i<npcRecordContextArray.Count()-1;i++)
                    {
                        var currNPCRecord = npcRecordContextArray[i].Record;
                        if(npcRecordContextArray[i].ModKey.Name.Contains("Bandit War"))
                        {
                            var masterCollection = MasterReferenceCollection.FromPath(state.DataFolderPath.Path+"\\"+npcRecordContextArray[i].ModKey, state.GameRelease);
                            List<int> filers = new List<int>();
                            for(int j=0;j<npcRecordContextArray.Count()-1;j++)
                            {
                                foreach (var master in masterCollection.Masters)
                                {
                                    if(master.Master.FileName.String==npcRecordContextArray[j].ModKey.ToString())
                                    {
                                        filers.Add(j);
                                    }
                                }
                            }
                            if(currNPCRecord.Name is not null)
                            {
                                if(!currNPCRecord.Name.Equals(bweWinningOverride.Name))
                                {
                                    bweOverride.Name = currNPCRecord.Name.DeepCopy();
                                }
                            }
                            if(!currNPCRecord.Configuration.Equals(bweWinningOverride.Configuration))
                            {
                                bweOverride.Configuration = currNPCRecord.Configuration.DeepCopy();
                            }
                            if(!currNPCRecord.DefaultOutfit.Equals(bweWinningOverride.DefaultOutfit))
                            {
                                bweOverride.DefaultOutfit.SetTo(currNPCRecord.DefaultOutfit);
                            }
                            if(!currNPCRecord.Template.Equals(bweWinningOverride.Template))
                            {
                                bweOverride.Template = currNPCRecord.Template.AsNullable();
                            }
                            if(!currNPCRecord.AIData.Equals(bweWinningOverride.AIData))
                            {
                                bweOverride.AIData = currNPCRecord.AIData.DeepCopy();
                            }
                            if(!currNPCRecord.Class.Equals(bweWinningOverride.Class))
                            {
                                bweOverride.Class.SetTo(currNPCRecord.Class);
                            }
                            if(!currNPCRecord.Class.Equals(bweWinningOverride.Class))
                            {
                                bweOverride.Class.SetTo(currNPCRecord.Class);
                            }
                            if(currNPCRecord.PlayerSkills is not null)
                            {
                                if(!currNPCRecord.PlayerSkills.Equals(bweWinningOverride.PlayerSkills))
                                {
                                    bweOverride.PlayerSkills = currNPCRecord.PlayerSkills.DeepCopy();
                                }
                            } 
                            if(!currNPCRecord.CombatStyle.Equals(bweWinningOverride.CombatStyle))
                            {
                                bweOverride.CombatStyle.SetTo(currNPCRecord.CombatStyle);
                            }
                            if(!currNPCRecord.DefaultPackageList.Equals(bweWinningOverride.DefaultPackageList))
                            {
                                bweOverride.DefaultPackageList.SetTo(currNPCRecord.DefaultPackageList);
                            }
                            if(currNPCRecord.ActorEffect is not null)
                            {
                                if(!currNPCRecord.ActorEffect.Equals(bweWinningOverride.ActorEffect))
                                {
                                    if(bweOverride.ActorEffect is not null){ bweOverride.ActorEffect.Clear();}
                                    if(bweOverride.ActorEffect is null)
                                    {
                                        bweOverride.ActorEffect = new Noggog.ExtendedList<IFormLinkGetter<ISpellRecordGetter>>();
                                    }
                                    if(currNPCRecord.ActorEffect is not null)
                                    {
                                        bweOverride.ActorEffect.AddRange(currNPCRecord.ActorEffect);
                                    }
                                }
                            }
                            if(!currNPCRecord.Packages.Equals(bweWinningOverride.Packages) && bweWinningOverride.Packages is not null)
                            {
                                var temp = bweOverride.Packages.Union(bweNPC.Packages);
                                bweOverride.Packages.Clear();
                                bweOverride.Packages.AddRange(temp);
                            }
                            else if(!currNPCRecord.Packages.Equals(bweWinningOverride.Packages) && bweWinningOverride.Packages is null && currNPCRecord.Packages is not null)
                            {
                                foreach(var pack in currNPCRecord.Packages)
                                {
                                    bweOverride.Packages.Add(pack);
                                }
                            }
                            if(currNPCRecord.Items is not null)
                            {
                                if(!ItemEquivalence(currNPCRecord.Items,bweOverride.Items))
                                {
                                    if(currNPCRecord.Items is not null)
                                    {
                                        if(bweOverride.Items is null){bweOverride.Items = new();}
                                        Noggog.ExtendedList<ContainerEntry>? tempce = new();
                                        tempce.AddRange(bweOverride.Items);
                                        foreach (var item in currNPCRecord.Items)
                                        {
                                            if(!tempce.Contains(item))
                                            {
                                                tempce.Add(item.DeepCopy());
                                            }
                                        }
                                        bweOverride.Items.Clear();
                                        foreach(var item in tempce)
                                        {
                                            bool toCopy = true;
                                            if(!currNPCRecord.Items.Contains(item))
                                            {
                                                for(int l=0;l<filers.Count;l++)
                                                {
                                                    var baseRecords = npcRecordContextArray[filers[l]].Record.Items;
                                                    if(baseRecords is not null)
                                                    {
                                                        if(baseRecords.Contains(item))
                                                        {
                                                            toCopy = false;
                                                        }
                                                    }
                                                }
                                            }
                                            if(toCopy){bweOverride.Items.Add(item.DeepCopy());}
                                        }
                                    }
                                }
                            }
                            if(currNPCRecord.Perks is not null)
                            {
                                if(!PerkEquivalence(currNPCRecord.Perks,bweOverride.Perks))
                                {
                                    if(currNPCRecord.Perks is not null)
                                    {
                                        if(bweOverride.Perks is null){bweOverride.Perks = new();}
                                        Noggog.ExtendedList<PerkPlacement>? tempce = new();
                                        tempce.AddRange(bweOverride.Perks);
                                        foreach (var perk in currNPCRecord.Perks)
                                        {
                                            if(!tempce.Contains(perk))
                                            {
                                                tempce.Add(perk.DeepCopy());
                                            }
                                        }
                                        bweOverride.Perks.Clear();
                                        foreach(var perk in tempce)
                                        {
                                            bool toCopy = true;
                                            if(!currNPCRecord.Perks.Contains(perk))
                                            {
                                                for(int l=0;l<filers.Count;l++)
                                                {
                                                    var baseRecords = npcRecordContextArray[filers[l]].Record.Perks;
                                                    if(baseRecords is not null)
                                                    {
                                                        foreach(var baseperk in baseRecords)
                                                        {
                                                            if(perk.Perk.Equals(baseperk.Perk))
                                                            {
                                                                toCopy=false;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            if(toCopy){bweOverride.Perks.Add(perk.DeepCopy());}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if(!BWEquivalence(bweOverride,bweWinningOverride))
                {
                    Console.WriteLine("Patched: "+bweOverride.EditorID);
                    state.PatchMod.Npcs.Add(bweOverride);
                }
            }

        }

    }
}
