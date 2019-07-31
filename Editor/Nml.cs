using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using System.Linq;

namespace TSKT.Tests
{
    public class Nml
    {
        const string sample = @"EquipableArticle ブロンズソード
        {
            displayName Article.ブロンズソード;
            description """";
            price 1000;
            equipmentType 剣;

            inEncyclopedia true;

            status
            {
                conditionFactors 剣;
                status
                {
                    攻撃 19;
                }
            }
            status
            {
                ability 剣;
                specialPowerTypes 剣装備;
            }
            replaceNameAbility 剣;
        }";

        [Test]
        public void Parse()
        {
            var nml = new TSKT.Nml();
            nml.Parse(sample);
            Assert.AreEqual("ブロンズソード", nml.SearchChild("EquipableArticle").Parameters[0]);
        }
    }
}

