using System;
using System.Collections.Generic;

[Serializable]
public class MergedCharacterMeta
{
    public List<CharacterMeta> variants;
    
    public CharacterMeta StandardVariant => variants[0];
    public bool HasOtherVariants => variants.Count > 1;
}