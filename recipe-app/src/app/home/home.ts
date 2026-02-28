import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { RecipeService } from '../services/recipe-service';
import { CategoryService } from '../services/category.service';
import { AuthService } from '../services/auth.service';
import { RecipeSummary, Category } from '../interface/recipes';
import { ImageUrlPipe } from '../pipes/image-url.pipe';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ImageUrlPipe],
  templateUrl: './home.html',
  styleUrl: './home.css'
})
export class Home implements OnInit {
  recipes    = signal<RecipeSummary[]>([]);
  categories = signal<Category[]>([]);
  total      = signal(0);
  page       = signal(1);
  pageSize   = 9;
  loading    = signal(true);
  error      = signal<string | null>(null);

  searchQuery = '';
  selectedCategory = signal<number | undefined>(undefined);

  isLoggedIn!:  AuthService['isLoggedIn'];
  currentUser!: AuthService['user'];

  constructor(
    private recipeService: RecipeService,
    private categoryService: CategoryService,
    private auth: AuthService
  ) {
    this.isLoggedIn  = this.auth.isLoggedIn;
    this.currentUser = this.auth.user;
  }

  ngOnInit(): void {
    this.categoryService.getAll().subscribe(c => this.categories.set(c));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = this.searchQuery.trim();
    const catId = this.selectedCategory();
    const src = (q || catId)
      ? this.recipeService.search(q, catId, this.page(), this.pageSize)
      : this.recipeService.getRecipes(this.page(), this.pageSize);

    src.subscribe({
      next: (res) => {
        this.recipes.set(res.recipes);
        this.total.set(res.total);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load recipes. Make sure the backend is running.');
        this.loading.set(false);
      }
    });
  }

  search(): void {
    this.page.set(1);
    this.load();
  }

  selectCategory(id: number | undefined): void {
    this.selectedCategory.set(id);
    this.page.set(1);
    this.load();
  }

  nextPage(): void {
    if (this.page() * this.pageSize < this.total()) {
      this.page.update(p => p + 1);
      this.load();
    }
  }

  prevPage(): void {
    if (this.page() > 1) {
      this.page.update(p => p - 1);
      this.load();
    }
  }

  get totalPages(): number {
    return Math.ceil(this.total() / this.pageSize);
  }

  logout(): void {
    this.auth.logout();
  }

  stars(avg: number): (string)[] {
    return [1,2,3,4,5].map(s => s <= Math.round(avg) ? 'filled' : 'empty');
  }
}
