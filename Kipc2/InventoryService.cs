using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;

namespace Kipc2
{
    using global::KRPC.SpaceCenter.Services.Parts;

    /// <summary>
    /// Service for accessing InventoryPart modules
    /// </summary>
    [KRPCService(GameScene = GameScene.All, Name = "Inventory")]
    public static class InventoryService
    {
        [KRPCProcedure]
        public static IList<string> ListInventory(Module module)
        {
            var _m = (ModuleInventoryPart)module.Part.InternalPart.Modules[module.Name];

            int slots = _m.InventorySlots;
            var inventoryList = new List<string>(new string[slots]);

            foreach (var slotIndex in _m.storedParts.Keys)
            {
                if (_m.storedParts.TryGetValue(slotIndex, out StoredPart storedPart))
                {
                    if (slotIndex >= 0 && slotIndex < slots)
                    {
                        inventoryList[slotIndex] = storedPart.partName;
                    }
                }
            }

            return inventoryList;
        }

        [KRPCProcedure]
        public static bool MoveInventory(Module from, Module to, int fromslot, int toslot)
        {

            var _from = (ModuleInventoryPart)from.Part.InternalPart.Modules[from.Name];
            var _to = (ModuleInventoryPart)to.Part.InternalPart.Modules[to.Name];


            if(fromslot >= _from.InventorySlots) return false;
            if(toslot >= _to.InventorySlots) return false;

            if(_from.IsSlotEmpty(fromslot)) return false;
            if(!_to.IsSlotEmpty(toslot)) return false;

            //var sPart = _from.storedParts.At(fromslot);
            StoredPart sPart = null;
            _from.storedParts.TryGetValue(fromslot, out sPart);
            //var idx_from = _from.storedParts.Keys.
            if (sPart == null) return false;

            _to.StoreCargoPartAtSlot(sPart.snapshot, toslot);
            _from.ClearPartAtSlot(fromslot);
            return true;
        }
    }
}
