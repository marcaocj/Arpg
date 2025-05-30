using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Classe de compatibilidade que herda de PlayerInventoryManager
/// Mantém compatibilidade com código existente que referencia "Inventory"
/// </summary>
public class Inventory : PlayerInventoryManager
{
    // Esta classe existe apenas para manter compatibilidade com código existente
    // que referencia "Inventory" em vez de "PlayerInventoryManager"
    
    // Todos os métodos e propriedades são herdados de PlayerInventoryManager
    // Métodos adicionais de compatibilidade podem ser adicionados aqui se necessário
    
    #region Legacy Compatibility Methods
    
    /// <summary>
    /// Método legacy - usar AddItem() do PlayerInventoryManager
    /// </summary>
    public bool AddItemLegacy(Item item)
    {
        return AddItem(item);
    }
    
    /// <summary>
    /// Método legacy - usar RemoveItem() do PlayerInventoryManager
    /// </summary>
    public bool RemoveItemLegacy(Item item)
    {
        return RemoveItem(item);
    }
    
    /// <summary>
    /// Propriedade legacy para compatibilidade
    /// </summary>
    public List<Item> Items => items;
    
    /// <summary>
    /// Método legacy para obter contagem de itens
    /// </summary>
    public int GetItemCount()
    {
        return CurrentItemCount;
    }
    
    /// <summary>
    /// Método legacy para verificar se tem espaço
    /// </summary>
    public bool CanAddItem()
    {
        return HasSpace;
    }
    
    #endregion
}