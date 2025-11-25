import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { api, Ingredient, RecipeIngredient } from '../services/api'

export default function RecipeCreator() {
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  
  // Available ingredients from database
  const [availableIngredients, setAvailableIngredients] = useState<Ingredient[]>([])
  const [loadingIngredients, setLoadingIngredients] = useState(true)
  
  // Form state
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [servings, setServings] = useState(1)
  const [prepTimeMin, setPrepTimeMin] = useState<number | ''>('')
  const [tags, setTags] = useState('')
  const [instructions, setInstructions] = useState('')
  
  // Ingredient management
  const [selectedIngredients, setSelectedIngredients] = useState<RecipeIngredient[]>([])
  const [currentIngredientId, setCurrentIngredientId] = useState<number | ''>('')
  const [currentQuantity, setCurrentQuantity] = useState<number | ''>('')
  
  // New ingredient form
  const [showNewIngredientForm, setShowNewIngredientForm] = useState(false)
  const [newIngredientName, setNewIngredientName] = useState('')
  const [newIngredientCalories, setNewIngredientCalories] = useState<number | ''>('')
  const [newIngredientProtein, setNewIngredientProtein] = useState<number | ''>('')
  const [newIngredientFat, setNewIngredientFat] = useState<number | ''>('')
  const [newIngredientCarbs, setNewIngredientCarbs] = useState<number | ''>('')

  useEffect(() => {
    fetchIngredients()
  }, [])

  const fetchIngredients = async () => {
    try {
      const ingredients = await api.getAllIngredients()
      setAvailableIngredients(ingredients)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load ingredients')
    } finally {
      setLoadingIngredients(false)
    }
  }

  const handleAddIngredient = () => {
    if (!currentIngredientId || !currentQuantity) {
      setError('Please select an ingredient and enter quantity')
      return
    }

    const ingredient = availableIngredients.find(i => i.ingredientId === Number(currentIngredientId))
    if (!ingredient) return

    const newIngredient: RecipeIngredient = {
      recipeId: 0, // Will be set by backend
      ingredientId: Number(currentIngredientId),
      quantityG: Number(currentQuantity),
      ingredientName: ingredient.name
    }

    setSelectedIngredients([...selectedIngredients, newIngredient])
    setCurrentIngredientId('')
    setCurrentQuantity('')
    setError(null)
  }

  const handleRemoveIngredient = (index: number) => {
    setSelectedIngredients(selectedIngredients.filter((_, i) => i !== index))
  }

  const calculateNutrition = () => {
    let totalCalories = 0
    let totalProtein = 0
    let totalFat = 0
    let totalCarbs = 0

    selectedIngredients.forEach(recipeIng => {
      const ingredient = availableIngredients.find(i => i.ingredientId === recipeIng.ingredientId)
      if (ingredient) {
        const multiplier = recipeIng.quantityG / 100
        totalCalories += (ingredient.caloriesPer100G || 0) * multiplier
        totalProtein += (ingredient.proteinPer100G || 0) * multiplier
        totalFat += (ingredient.fatPer100G || 0) * multiplier
        totalCarbs += (ingredient.carbsPer100G || 0) * multiplier
      }
    })

    return {
      calories: Math.round(totalCalories),
      protein: Math.round(totalProtein * 10) / 10,
      fat: Math.round(totalFat * 10) / 10,
      carbs: Math.round(totalCarbs * 10) / 10
    }
  }

  const handleCreateNewIngredient = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!newIngredientName.trim()) {
      setError('Ingredient name is required')
      return
    }

    try {
      const result = await api.createIngredient({
        name: newIngredientName.trim(),
        caloriesPer100G: newIngredientCalories ? Number(newIngredientCalories) : undefined,
        proteinPer100G: newIngredientProtein ? Number(newIngredientProtein) : undefined,
        fatPer100G: newIngredientFat ? Number(newIngredientFat) : undefined,
        carbsPer100G: newIngredientCarbs ? Number(newIngredientCarbs) : undefined,
      })

      // Refresh ingredients list
      await fetchIngredients()
      
      // Reset form
      setShowNewIngredientForm(false)
      setNewIngredientName('')
      setNewIngredientCalories('')
      setNewIngredientProtein('')
      setNewIngredientFat('')
      setNewIngredientCarbs('')
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create ingredient')
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    
    if (!title.trim()) {
      setError('Recipe title is required')
      return
    }

    if (servings < 1) {
      setError('Servings must be at least 1')
      return
    }

    if (selectedIngredients.length === 0) {
      setError('Please add at least one ingredient')
      return
    }

    setLoading(true)

    try {
      const nutrition = calculateNutrition()
      
      const result = await api.createRecipe({
        title: title.trim(),
        description: description.trim() || undefined,
        totalCalories: nutrition.calories,
        proteinG: nutrition.protein,
        fatG: nutrition.fat,
        carbsG: nutrition.carbs,
        servings,
        prepTimeMin: prepTimeMin ? Number(prepTimeMin) : undefined,
        tags: tags.trim() || undefined,
        instructions: instructions.trim() || undefined,
        ingredients: selectedIngredients
      })

      setSuccess(true)
      setTimeout(() => {
        navigate('/recipes')
      }, 2000)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create recipe')
      setLoading(false)
    }
  }

  const nutrition = calculateNutrition()

  if (loadingIngredients) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading ingredients...</p>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <button
          onClick={() => navigate('/recipes')}
          className="mb-4 text-blue-600 hover:text-blue-800 font-medium flex items-center"
        >
          ← Back to Recipes
        </button>

        <div className="bg-white shadow rounded-lg p-6">
          <h1 className="text-3xl font-bold text-gray-900 mb-6">Create New Recipe</h1>

          {error && (
            <div className="mb-6 bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
              {error}
            </div>
          )}

          {success && (
            <div className="mb-6 bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg">
              Recipe created successfully! Redirecting...
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Basic Information */}
            <div>
              <h2 className="text-xl font-semibold mb-4">Basic Information</h2>
              
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Recipe Title *
                  </label>
                  <input
                    type="text"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="e.g., Grilled Chicken Breast"
                    required
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <textarea
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    rows={3}
                    placeholder="Brief description of the recipe..."
                  />
                </div>

                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Servings *
                    </label>
                    <input
                      type="number"
                      value={servings}
                      onChange={(e) => setServings(Number(e.target.value))}
                      min="1"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      required
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Prep Time (min)
                    </label>
                    <input
                      type="number"
                      value={prepTimeMin}
                      onChange={(e) => setPrepTimeMin(e.target.value ? Number(e.target.value) : '')}
                      min="0"
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Tags
                    </label>
                    <input
                      type="text"
                      value={tags}
                      onChange={(e) => setTags(e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      placeholder="e.g., high-protein"
                    />
                  </div>
                </div>
              </div>
            </div>

            {/* Ingredients */}
            <div>
              <div className="flex justify-between items-center mb-4">
                <h2 className="text-xl font-semibold">Ingredients *</h2>
                <button
                  type="button"
                  onClick={() => setShowNewIngredientForm(!showNewIngredientForm)}
                  className="text-sm text-blue-600 hover:text-blue-800 font-medium"
                >
                  {showNewIngredientForm ? 'Cancel' : '+ Add New Ingredient'}
                </button>
              </div>

              {showNewIngredientForm && (
                <div className="mb-4 p-4 bg-gray-50 border border-gray-200 rounded-lg">
                  <h3 className="font-medium mb-3">Create New Ingredient</h3>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="col-span-2">
                      <input
                        type="text"
                        value={newIngredientName}
                        onChange={(e) => setNewIngredientName(e.target.value)}
                        placeholder="Ingredient name *"
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                    <input
                      type="number"
                      value={newIngredientCalories}
                      onChange={(e) => setNewIngredientCalories(e.target.value ? Number(e.target.value) : '')}
                      placeholder="Calories per 100g"
                      className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                    <input
                      type="number"
                      value={newIngredientProtein}
                      onChange={(e) => setNewIngredientProtein(e.target.value ? Number(e.target.value) : '')}
                      placeholder="Protein per 100g"
                      className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                    <input
                      type="number"
                      value={newIngredientFat}
                      onChange={(e) => setNewIngredientFat(e.target.value ? Number(e.target.value) : '')}
                      placeholder="Fat per 100g"
                      className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                    <input
                      type="number"
                      value={newIngredientCarbs}
                      onChange={(e) => setNewIngredientCarbs(e.target.value ? Number(e.target.value) : '')}
                      placeholder="Carbs per 100g"
                      className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                    />
                  </div>
                  <button
                    type="button"
                    onClick={handleCreateNewIngredient}
                    className="mt-3 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
                  >
                    Create Ingredient
                  </button>
                </div>
              )}

              <div className="flex gap-3 mb-4">
                <select
                  value={currentIngredientId}
                  onChange={(e) => setCurrentIngredientId(e.target.value ? Number(e.target.value) : '')}
                  className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="">Select ingredient...</option>
                  {availableIngredients.map(ing => (
                    <option key={ing.ingredientId} value={ing.ingredientId}>
                      {ing.name}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  value={currentQuantity}
                  onChange={(e) => setCurrentQuantity(e.target.value ? Number(e.target.value) : '')}
                  placeholder="Quantity (g)"
                  className="w-32 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  type="button"
                  onClick={handleAddIngredient}
                  className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700"
                >
                  Add
                </button>
              </div>

              {selectedIngredients.length > 0 && (
                <div className="border border-gray-200 rounded-lg overflow-hidden">
                  <table className="w-full">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-2 text-left text-sm font-medium text-gray-700">Ingredient</th>
                        <th className="px-4 py-2 text-right text-sm font-medium text-gray-700">Quantity (g)</th>
                        <th className="px-4 py-2"></th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {selectedIngredients.map((ing, index) => (
                        <tr key={index}>
                          <td className="px-4 py-2 text-sm">{ing.ingredientName}</td>
                          <td className="px-4 py-2 text-sm text-right">{ing.quantityG}g</td>
                          <td className="px-4 py-2 text-right">
                            <button
                              type="button"
                              onClick={() => handleRemoveIngredient(index)}
                              className="text-red-600 hover:text-red-800 text-sm"
                            >
                              Remove
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {/* Nutritional Information (Auto-calculated) */}
            {selectedIngredients.length > 0 && (
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                <h3 className="font-semibold text-gray-900 mb-3">
                  Calculated Nutrition (Total Recipe)
                </h3>
                <div className="grid grid-cols-4 gap-4">
                  <div>
                    <div className="text-2xl font-bold text-blue-600">{nutrition.calories}</div>
                    <div className="text-sm text-gray-600">Calories</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-blue-600">{nutrition.protein}g</div>
                    <div className="text-sm text-gray-600">Protein</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-blue-600">{nutrition.fat}g</div>
                    <div className="text-sm text-gray-600">Fat</div>
                  </div>
                  <div>
                    <div className="text-2xl font-bold text-blue-600">{nutrition.carbs}g</div>
                    <div className="text-sm text-gray-600">Carbs</div>
                  </div>
                </div>
                <div className="mt-3 text-sm text-gray-600">
                  Per serving ({servings} serving{servings > 1 ? 's' : ''}): 
                  <strong> {Math.round(nutrition.calories / servings)} cal</strong>
                </div>
              </div>
            )}

            {/* Instructions */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Cooking Instructions
              </label>
              <textarea
                value={instructions}
                onChange={(e) => setInstructions(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                rows={6}
                placeholder="Step-by-step cooking instructions..."
              />
            </div>

            {/* Submit Button */}
            <div className="flex justify-end gap-3">
              <button
                type="button"
                onClick={() => navigate('/recipes')}
                className="px-6 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50"
                disabled={loading}
              >
                Cancel
              </button>
              <button
                type="submit"
                className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:bg-gray-400"
                disabled={loading || selectedIngredients.length === 0}
              >
                {loading ? 'Creating...' : 'Create Recipe'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

