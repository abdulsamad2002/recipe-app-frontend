import { Component } from '@angular/core';
import { RecipeService } from '../../services/recipe-service';
import { Recipe } from '../../interface/recipes';
import { CommonModule } from '@angular/common';

/**
 * Simple recipe-card component kept for backward compatibility.
 * The main feed is now driven by the Home page component.
 */
@Component({
  selector: 'app-recipe-card',
  imports: [CommonModule],
  templateUrl: './recipe-card.html',
  styleUrl: './recipe-card.css',
})
export class RecipeCard {
  constructor(private RecipeService: RecipeService) {}
  recipes: Recipe[] = [];

  recipeGetter(): void {
    this.RecipeService.getRecipes().subscribe((res) => {
      this.recipes = res.recipes as unknown as Recipe[];
      console.log(this.recipes);
    });
  }

  ngOnInit() {
    this.recipeGetter();
  }
}
