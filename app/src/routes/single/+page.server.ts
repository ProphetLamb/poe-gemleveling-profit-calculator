import { fail, type Actions } from '@sveltejs/kit';
import { API_KEY, API_ENDPOINT } from '$env/static/private';
import { createGemProfitApi } from '$lib/server/gemLevelProfitApi';
import { superValidate } from 'sveltekit-superforms/client';
import { gemProfitRequestParameterSchema } from '$lib/gemLevelProfitApi';
import type { PageServerLoad } from './$types';

export const load: PageServerLoad = async ({ request }) => {
	const gemLevelsProfitForm = await superValidate(request, gemProfitRequestParameterSchema);
	return { gemLevelsProfitForm };
};

export const actions: Actions = {
	default: async ({ request, fetch }) => {
		const gemLevelsProfitForm = await superValidate(request, gemProfitRequestParameterSchema);
		let response = { gemLevelsProfitForm }

		if (!gemLevelsProfitForm.valid) {
			return fail(400, response);
		}

		const gemProfitApi = createGemProfitApi(fetch, {
			api_endpoint: API_ENDPOINT,
			api_key: API_KEY
		});

		try {
			const gemProfit = await gemProfitApi.getGemProfit(gemLevelsProfitForm.data);
			return { ...response, gemProfit };
		} catch (error) {
			return fail(500, response);
		}
	}
};
