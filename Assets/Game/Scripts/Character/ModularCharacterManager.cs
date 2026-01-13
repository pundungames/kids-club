using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModularCharacterManager : MonoBehaviour
{
    [Header("Asset Havuzu")]
    public List<ModularPart> allParts;

    private PartType[] mandatoryTypes = new PartType[]
    {
        PartType.Face,
        PartType.Hair,
        PartType.UpperBody,
        PartType.LowerBody,
        PartType.Outfit
    };

    public void BuildCharacter(Gender preferredGender, SkinType preferredSkin)
    {
        // 1. Karakteri Belirle
        Gender finalGender = preferredGender;
        if (preferredGender == Gender.Both)
            finalGender = (Random.Range(0, 2) == 0) ? Gender.Male : Gender.Female;

        SkinType finalSkin = preferredSkin;
        if (preferredSkin == SkinType.Both)
            finalSkin = (Random.Range(0, 2) == 0) ? SkinType.Light : SkinType.Brown;

        // 2. Temizlik
        foreach (var part in allParts)
        {
            if (part.obj != null) part.obj.SetActive(false);
        }

        // 3. Seçim Baþlasýn
        // Debug.Log($"KARAKTER OLUÞUYOR: {finalGender} - {finalSkin}"); // Ýstersen açabilirsin

        foreach (PartType type in mandatoryTypes)
        {
            SelectPartGuaranteed(type, finalGender, finalSkin);
        }
    }

    void SelectPartGuaranteed(PartType type, Gender targetGender, SkinType targetSkin)
    {
        // ADIM 1: MÜKEMMEL EÞLEÞME ARA
        // Kural: Gender == Hedef veya Both OLSUN --- VE --- Skin == Hedef veya Both OLSUN
        var candidates = allParts.Where(x =>
            x.type == type &&
            (x.gender == targetGender || x.gender == Gender.Both) &&
            (x.skin == targetSkin || x.skin == SkinType.Both)
        ).ToList();

        // Eðer mükemmel eþleþme bulamazsan...
        if (candidates.Count == 0)
        {
            // ADIM 2: RENGÝ BOÞVER (Sadece Cinsiyet Tutsun)
            // Zenci Kadýn Gövdesi yoksa, Beyaz Kadýn Gövdesi koyalým.
            candidates = allParts.Where(x =>
                x.type == type &&
                (x.gender == targetGender || x.gender == Gender.Both)
            ).ToList();

            if (candidates.Count > 0)
            {
                Debug.LogWarning($"DÝKKAT: '{targetGender} - {targetSkin}' karakteri için '{type}' parçasýnda TAM EÞLEÞME bulunamadý! Rengi önemsemeden baþka bir {type} seçildi.");
            }
        }

        // Eðer hala bulamazsan...
        if (candidates.Count == 0)
        {
            // ADIM 3: CÝNSÝYETÝ DE BOÞVER (Ne varsa getir)
            // Kadýn Gövdesi hiç yoksa, Erkek Gövdesi koy (Boþ kalmasýndan iyidir, hatayý gözünle görürsün).
            candidates = allParts.Where(x => x.type == type).ToList();

            if (candidates.Count > 0)
            {
                Debug.LogError($"KRÝTÝK: '{targetGender}' için listede HÝÇ '{type}' yok! Mecburen rastgele (Erkek/Both) bir parça seçildi.");
            }
        }

        // SONUÇ: SEÇ VE AÇ
        if (candidates.Count > 0)
        {
            var chosen = candidates[Random.Range(0, candidates.Count)];
            if (chosen.obj != null) chosen.obj.SetActive(true);
        }
        else
        {
            // Buraya düþüyorsa Inspector listende o TÜRDE (örn: UpperBody) tek bir obje bile yok demektir.
            Debug.LogError($"HATA: Listede HÝÇ '{type}' türünde obje yok! Karakterin bu parçasý eksik.");
        }
    }
}