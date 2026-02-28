import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RecipeService } from '../../services/recipe-service';
import { CommentService } from '../../services/comment.service';
import { RatingService } from '../../services/rating.service';
import { AuthService } from '../../services/auth.service';
import { Recipe, Comment } from '../../interface/recipes';
import { ImageUrlPipe } from '../../pipes/image-url.pipe';

@Component({
  selector: 'app-recipe-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ImageUrlPipe],
  templateUrl: './recipe-detail.html',
  styleUrl: './recipe-detail.css'
})
export class RecipeDetail implements OnInit {
  recipe  = signal<Recipe | null>(null);
  comments = signal<Comment[]>([]);
  loading  = signal(true);
  error    = signal<string | null>(null);
  newComment = '';
  commentError = signal<string | null>(null);
  commentLoading = signal(false);
  userRating = signal(0);
  hoverRating = signal(0);
  ratingLoading = signal(false);

  readonly isLoggedIn = computed(() => this.auth.isLoggedIn());
  readonly currentUser = computed(() => this.auth.user());

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private recipeService: RecipeService,
    private commentService: CommentService,
    private ratingService: RatingService,
    private auth: AuthService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) { this.router.navigate(['/']); return; }
    this.loadRecipe(id);
    this.loadComments(id);
  }

  loadRecipe(id: number): void {
    this.loading.set(true);
    this.recipeService.getById(id).subscribe({
      next: (r) => { this.recipe.set(r); this.loading.set(false); },
      error: () => { this.error.set('Recipe not found.'); this.loading.set(false); }
    });
  }

  loadComments(id: number): void {
    this.commentService.getForRecipe(id).subscribe({
      next: (c) => this.comments.set(c)
    });
  }

  submitComment(): void {
    const r = this.recipe();
    if (!r || !this.newComment.trim()) return;
    this.commentLoading.set(true);
    this.commentService.create({ recipeId: r.recipeId, body: this.newComment.trim() }).subscribe({
      next: (c) => {
        this.comments.update(list => [c, ...list]);
        this.newComment = '';
        this.commentLoading.set(false);
      },
      error: (err) => {
        this.commentError.set(err?.error?.message ?? 'Failed to post comment.');
        this.commentLoading.set(false);
      }
    });
  }

  deleteComment(id: number): void {
    this.commentService.delete(id).subscribe(() => {
      this.comments.update(list => list.filter(c => c.commentId !== id));
    });
  }

  setRating(score: number): void {
    const r = this.recipe();
    if (!r || !this.isLoggedIn()) return;
    this.ratingLoading.set(true);
    this.ratingService.rate({ recipeId: r.recipeId, score }).subscribe({
      next: () => {
        this.userRating.set(score);
        this.ratingLoading.set(false);
        // Refresh recipe to get updated averageRating
        this.loadRecipe(r.recipeId);
      },
      error: () => this.ratingLoading.set(false)
    });
  }

  deleteRecipe(): void {
    const r = this.recipe();
    if (!r) return;
    if (!confirm('Delete this recipe? This cannot be undone.')) return;
    this.recipeService.delete(r.recipeId).subscribe({
      next: () => this.router.navigate(['/'])
    });
  }

  isOwner(): boolean {
    const u = this.currentUser();
    const r = this.recipe();
    return !!u && !!r && u.userId === r.userId;
  }

  stars = [1, 2, 3, 4, 5];
}
