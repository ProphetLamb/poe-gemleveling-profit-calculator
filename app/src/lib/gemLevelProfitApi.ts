import { z } from 'zod';

export interface GemProfitRequestParameter {
	league: string;
	gem_name?: string | null;
	min_sell_price_chaos?: number | null;
	max_buy_price_chaos?: number | null;
	min_experience_delta?: number | null;
	items_offset?: number | null;
	items_count?: number | null;
}

export const gemProfitRequestParameterSchema = z.object({
	league: z.string(),
	gem_name: z.string().nullable().optional(),
	min_sell_price_chaos: z.number().nullable().optional(),
	max_buy_price_chaos: z.number().nullable().optional(),
	min_experience_delta: z.number().min(200000000).max(2000000000).default(340000000),
	items_offset: z.number().nullable().optional().default(0),
	items_count: z.number().nullable().optional().default(10)
});

export interface GemProfitResponseItemPrice {
	price: number;
	quality: number;
	level: number;
	experience: number;
	listing_count: number;
}

export const gemProfitResponseItemPriceSchema = z.object({
	price: z.number(),
	quality: z.number(),
	level: z.number(),
	experience: z.number(),
	listing_count: z.number()
})

export type GemColor = "white" | "blue" | "green" | "red";
export const gemColorSchema = z.enum(["white", "blue", "green", "red"]);

export interface ProfitMargin {
	adjusted_earnings: number;
	experience_delta: number;
	gain_margin: number;
	quality_spent: number;
}

export const profitMarginSchema = z.object({
	adjusted_earnings: z.number(),
	experience_delta: z.number(),
	gain_margin: z.number(),
	quality_spent: z.number(),
})

export interface ProfitResponseRecipes {
	quality_then_level: ProfitMargin;
	level_vendor_level: ProfitMargin;
}

export const profitResponseRecipesSchema = z.object({
	quality_then_level: profitMarginSchema,
	level_vendor_level: profitMarginSchema,
})

export interface GemProfitResponseItem {
	name: string,
	icon?: string | null,
	color: GemColor;
	min: GemProfitResponseItemPrice;
	max: GemProfitResponseItemPrice;
	gain_margin: number;
	type: string;
	discriminator?: string | null;
	foreign_info_url: string;
	recipes: ProfitResponseRecipes;
}

export const gemProfitResponseItemSchema = z.object({
	name: z.string(),
	icon: z.string().nullable().optional(),
	color: gemColorSchema,
	min: gemProfitResponseItemPriceSchema,
	max: gemProfitResponseItemPriceSchema,
	gain_margin: z.number(),
	type: z.string(),
	discriminator: z.string().nullable().optional(),
	foreign_info_url: z.string(),
	recipes: profitResponseRecipesSchema
});

export type GemProfitResponse = GemProfitResponseItem[]

export const gemProfitResponseSchema = z.array(gemProfitResponseItemSchema)