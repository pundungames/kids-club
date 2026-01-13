using UnityEngine;

// 1. GENDER COMPATIBILITY (Sadece bunlar geçerli)
public enum Gender { Male, Female, Both }

// 2. SKIN TONE COMPATIBILITY (Sadece bunlar geçerli)
// DÝKKAT: 'Dark' yerine senin kuralýn olan 'Brown' yazýldý.
public enum SkinType { Light, Brown, Both }

// 3. ASSET TYPE (Sadece bunlar geçerli)
public enum PartType { Face, Hair, UpperBody, LowerBody, Outfit }

[System.Serializable]
public class ModularPart
{
    public string name;      // Buraya istediðini yaz (Sadece editörde görmek için)
    public GameObject obj;   // Model Dosyasý
    public PartType type;    // Asset Type
    public Gender gender;    // Gender Compatibility
    public SkinType skin;    // Skin Tone Compatibility
}